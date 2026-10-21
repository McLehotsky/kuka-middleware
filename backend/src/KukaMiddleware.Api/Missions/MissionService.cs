using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Missions;

public class MissionService(IMissionStore store, TimeProvider time) : IMissionService
{
    // Phase 6 has no authentication: every mission belongs to the seeded "web-ui" client.
    // Phase 8 replaces this with the authenticated ApiClient.
    private const int WebUiClientId = 1;
    private const int DefaultPriority = 2;

    public async Task<CreateMissionResult> CreateAsync(CreateMissionRequest request, CancellationToken ct)
    {
        var errors = MissionValidation.Validate(request);
        if (request.RobotId is int robotId && !await store.RobotExistsAsync(robotId, ct))
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
            RobotId = request.RobotId,
            CreatedByClientId = WebUiClientId,
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
        return new CreateMissionResult.Created(new CreateMissionResponse(mission.Id, mission.MissionCode));
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

        // Phase 5 calls KUKA FM cancelMission before this local transition (system-definition §5, Q-B).
        mission.StatusHistory.Add(new MissionStatusHistory
        {
            PreviousStatus = mission.Status,
            Status = MissionStatus.Cancelling,
            Source = StatusSource.Api,
            Message = "cancel requested",
            ChangedAt = time.GetUtcNow(),
        });
        mission.Status = MissionStatus.Cancelling;
        await store.SaveChangesAsync(ct);
        return new CancelMissionResult.Cancelled(ToSummary(mission));
    }

    private static MissionSummary ToSummary(Mission m) => new(
        m.Id, m.MissionCode, m.ExternalRef, m.MissionType, m.Priority, m.Status, m.Robot?.Name, m.CreatedAt, m.LastSyncedAt);
}
