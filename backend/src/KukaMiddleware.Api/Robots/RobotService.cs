using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;

namespace KukaMiddleware.Api.Robots;

public class RobotService(IRobotStore store) : IRobotService
{
    public async Task<IReadOnlyList<RobotSummary>> ListAsync(CancellationToken ct)
    {
        var robots = await store.ListAsync(ct);
        var missions = await store.ListActiveMissionsAsync(ct);

        // Current mission = the robot's newest non-final mission (system-definition §8, Q-N).
        var current = missions
            .Where(m => m.RobotId is not null && !MissionStatuses.Final.Contains(m.Status))
            .GroupBy(m => m.RobotId!.Value)
            .ToDictionary(g => g.Key, g => g.MaxBy(m => m.CreatedAt)!);

        return [.. robots
            .OrderBy(r => string.IsNullOrWhiteSpace(r.Name) ? r.KukaRobotId : r.Name, StringComparer.Ordinal)
            .Select(r => ToSummary(r, current.GetValueOrDefault(r.Id)))];
    }

    private static RobotSummary ToSummary(Robot r, Mission? current) => new(
        r.Id, r.KukaRobotId, r.Name, r.Status, r.BatteryLevel, r.CurrentNode,
        current is null ? null : new RobotSummary.MissionRef(current.Id, current.MissionCode),
        r.LastSeenAt, r.UpdatedAt);
}
