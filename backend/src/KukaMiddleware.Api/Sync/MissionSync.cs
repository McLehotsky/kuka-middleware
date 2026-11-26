using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Missions;
using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Sync;

// UC5: poll the KUKA FM job of every active mission and apply the allowed status changes (system-definition §5).
// All changes of a cycle are saved in one SaveChanges (one transaction); a failed call or a concurrency
// conflict saves nothing and the next cycle retries.
public sealed class MissionSync(IKukaClient kuka, ISyncStore store, TimeProvider time, ILogger<MissionSync> logger) : ISyncJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var missions = await store.ListActiveMissionsAsync(ct);
        if (missions.Count == 0)
            return;

        var now = time.GetUtcNow();
        var changed = 0;
        foreach (var mission in missions)
        {
            var result = await kuka.QueryJobsAsync(mission.MissionCode, ct);
            if (!result.Succeeded)
            {
                logger.LogWarning("Mission sync aborted, nothing saved; retry in the next cycle: {Error}", result.Error);
                return;
            }

            mission.LastSyncedAt = now;
            var job = result.Data!.FirstOrDefault(j => j.JobCode == mission.MissionCode);
            if (job is null)
            {
                logger.LogWarning("Mission sync: no KUKA FM job for mission {MissionCode}; status unchanged", mission.MissionCode);
                continue;
            }

            if (mission.RobotId is null && !string.IsNullOrEmpty(job.RobotId))
                mission.RobotId = await store.FindRobotIdAsync(job.RobotId, ct);
            if (Apply(mission, job.Status, now))
                changed++;
        }

        try
        {
            await store.SaveChangesAsync(ct);
            if (changed > 0)
                logger.LogInformation("Mission sync changed the status of {ChangedCount} missions", changed);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Mission sync: a mission changed concurrently; nothing saved, retry in the next cycle");
        }
    }

    /// <returns>True when the status changed.</returns>
    private bool Apply(Mission mission, int jobStatus, DateTimeOffset now)
    {
        if (MissionStatuses.FromKukaJob(jobStatus) is not { } status)
        {
            logger.LogWarning("Mission sync: mission {MissionCode} has unknown KUKA job status {JobStatus}; status unchanged",
                mission.MissionCode, jobStatus);
            return false;
        }
        // Only steps back are not allowed (e.g. KUKA FM still executing while we are Cancelling): keep the status.
        if (!MissionStatuses.SyncCanChange(mission.Status, status))
            return false;

        mission.ChangeStatus(status, StatusSource.Sync, $"KUKA FM job status {jobStatus}", now);
        return true;
    }
}
