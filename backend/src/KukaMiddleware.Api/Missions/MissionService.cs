using KukaMiddleware.Api.Auth;
using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Missions;

public class MissionService(IMissionStore store, IKukaClient kuka, TimeProvider time, CurrentApiClient client) : IMissionService
{
    private const int DefaultPriority = 2;

    public async Task<CreateMissionResult> CreateAsync(CreateMissionRequest request, CancellationToken ct)
    {
        var errors = MissionValidation.Validate(request);
        Robot? robot = null;
        if (request.RobotId is int robotId && (robot = await store.FindRobotAsync(robotId, ct)) is null)
            errors["robotId"] = ["Robot does not exist."];
        if (errors.Count > 0)
            return new CreateMissionResult.Invalid(errors);

        var now = time.GetUtcNow();
        var number = await store.NextMissionCodeNumberAsync(ct);
        var mission = new Mission
        {
            Id = Guid.CreateVersion7(now),
            MissionCode = $"M-{now.Year}-{number:D4}",
            ExternalRef = request.ExternalRef,
            MissionType = request.MissionType!,
            Priority = request.Priority ?? DefaultPriority,
            Status = MissionStatus.Created,
            RobotId = robot?.Id,
            Robot = robot,
            CreatedByClientId = client.Id,
            CreatedAt = now,
        };
        mission.Steps.AddRange(request.Steps!.Select((step, i) =>
            new MissionStep { Sequence = i + 1, NodeCode = step.NodeCode!, Action = step.Action! }));
        mission.StatusHistory.Add(new MissionStatusHistory
        {
            Status = MissionStatus.Created,
            Source = StatusSource.Api,
            ChangedAt = now,
        });

        store.Add(mission);
        await store.SaveChangesAsync(ct);

        // From here on the mission exists: its outcome is recorded even if the caller disconnects.
        var submitted = await kuka.SubmitMissionAsync(mission, CancellationToken.None);
        var at = time.GetUtcNow();
        if (submitted.Succeeded)
        {
            mission.ChangeStatus(MissionStatus.Submitted, StatusSource.Api, null, at);
            mission.SubmittedAt = at;
        }
        else
        {
            mission.ChangeStatus(MissionStatus.Failed, StatusSource.Api, submitted.Error, at);
            mission.ErrorMessage = MissionStatuses.Truncate(submitted.Error);
        }
        try
        {
            await store.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A cancel won in between; the stored mission is Cancelling and the mission sync takes over.
        }

        return submitted.Succeeded
            ? new CreateMissionResult.Created(new CreateMissionResponse(mission.Id, mission.MissionCode))
            : new CreateMissionResult.KukaFailed(mission.Id, submitted.Error!);
    }

    public async Task<IReadOnlyList<MissionSummary>?> ListAsync(string? state, CancellationToken ct)
    {
        MissionStatus[]? statuses;
        switch (state)
        {
            case null or "": statuses = null; break;
            case "active": statuses = MissionStatuses.NotFinal; break;
            case "finished": statuses = MissionStatuses.Final; break;
            default: return null;
        }

        var missions = await store.ListAsync(statuses, ct);
        return [.. missions.OrderByDescending(m => m.CreatedAt).Select(ToSummary)];
    }

    public async Task<MissionDetail?> GetAsync(Guid id, CancellationToken ct) =>
        await store.FindAsync(id, ct) is { } m
            ? new MissionDetail(
                m.Id, m.MissionCode, m.ExternalRef, m.MissionType, m.Priority, m.Status, m.Robot?.Name,
                m.CreatedAt, m.LastSyncedAt, m.SubmittedAt, m.FinishedAt, m.ErrorMessage, m.CreatedByClient.Name,
                [.. m.Steps.OrderBy(s => s.Sequence).Select(s => new MissionStepDto(s.Sequence, s.NodeCode, s.Action))])
            : null;

    public async Task<IReadOnlyList<MissionHistoryEntry>?> GetHistoryAsync(Guid id, CancellationToken ct)
    {
        if (!await store.MissionExistsAsync(id, ct))
            return null;

        var rows = await store.ListHistoryAsync(id, ct);
        return [.. rows.OrderBy(h => h.ChangedAt)
            .Select(h => new MissionHistoryEntry(h.PreviousStatus, h.Status, h.Source, h.Message, h.ChangedAt))];
    }

    public async Task<CancelMissionResult> CancelAsync(Guid id, CancellationToken ct)
    {
        var mission = await store.FindAsync(id, ct);
        if (mission is null)
            return new CancelMissionResult.NotFound();
        if (!MissionStatuses.Cancellable.Contains(mission.Status))
            return new CancelMissionResult.Conflict(mission.Status);

        // Once the cancel is sent, its outcome is recorded even if the caller disconnects.
        var cancelled = await kuka.CancelMissionAsync(mission.MissionCode, CancellationToken.None);
        if (!cancelled.Succeeded)
            return new CancelMissionResult.KukaFailed(cancelled.Error!);

        mission.ChangeStatus(MissionStatus.Cancelling, StatusSource.Api, "cancel requested", time.GetUtcNow());
        try
        {
            await store.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Q11: usually the mission sync only wrote LastSyncedAt. Reload once and re-apply while still cancellable.
            await store.ReloadAsync(mission, CancellationToken.None);
            if (!MissionStatuses.Cancellable.Contains(mission.Status))
                return new CancelMissionResult.ConcurrentUpdate();
            mission.ChangeStatus(MissionStatus.Cancelling, StatusSource.Api, "cancel requested", time.GetUtcNow());
            try
            {
                await store.SaveChangesAsync(CancellationToken.None);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new CancelMissionResult.ConcurrentUpdate();
            }
        }
        return new CancelMissionResult.Cancelled(ToSummary(mission));
    }

    private static MissionSummary ToSummary(Mission m) => new(
        m.Id, m.MissionCode, m.ExternalRef, m.MissionType, m.Priority, m.Status, m.Robot?.Name, m.CreatedAt, m.LastSyncedAt);
}
