using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Missions;

public interface IMissionService
{
    Task<CreateMissionResult> CreateAsync(CreateMissionRequest request, CancellationToken ct);

    /// <summary>Null when <paramref name="state"/> is not <c>active</c>, <c>finished</c> or empty.</summary>
    Task<IReadOnlyList<MissionSummary>?> ListAsync(string? state, CancellationToken ct);

    Task<MissionDetail?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Null when the mission does not exist.</summary>
    Task<IReadOnlyList<MissionHistoryEntry>?> GetHistoryAsync(Guid id, CancellationToken ct);

    Task<CancelMissionResult> CancelAsync(Guid id, CancellationToken ct);
}

public abstract record CreateMissionResult
{
    public sealed record Created(CreateMissionResponse Mission) : CreateMissionResult;
    public sealed record Invalid(Dictionary<string, string[]> Errors) : CreateMissionResult;

    /// <summary>KUKA FM rejected the mission or was unreachable; the mission is stored as <c>Failed</c>.</summary>
    public sealed record KukaFailed(Guid MissionId, string Error) : CreateMissionResult;
}

public abstract record CancelMissionResult
{
    public sealed record Cancelled(MissionSummary Mission) : CancelMissionResult;
    public sealed record NotFound : CancelMissionResult;
    public sealed record Conflict(MissionStatus Status) : CancelMissionResult;

    /// <summary>The mission changed between the check and the save (Q11).</summary>
    public sealed record ConcurrentUpdate : CancelMissionResult;

    /// <summary>KUKA FM did not accept the cancel; the mission is unchanged.</summary>
    public sealed record KukaFailed(string Error) : CancelMissionResult;
}
