using KukaMiddleware.Api.Data.Entities;
using static KukaMiddleware.Api.Data.Entities.MissionStatus;

namespace KukaMiddleware.Api.Missions;

// Status sets and the state machine from system-definition §5; KUKA job mapping from §14 Phase 5 scope.
public static class MissionStatuses
{
    public const int MessageMaxLength = 1000;

    public static readonly MissionStatus[] Final = [Completed, Cancelled, Failed];
    public static readonly MissionStatus[] NotFinal = [Created, Submitted, InProgress, Cancelling];
    public static readonly MissionStatus[] Cancellable = [Created, Submitted, InProgress];

    /// <summary>Missions polled by the mission sync (UC5).</summary>
    public static readonly MissionStatus[] Active = [Submitted, InProgress, Cancelling];

    private static readonly Dictionary<MissionStatus, MissionStatus[]> SyncTransitions = new()
    {
        // KUKA FM is the source of truth, including cancels and errors from the KUKA FM UI.
        [Submitted] = [InProgress, Cancelling, Completed, Cancelled, Failed],
        [InProgress] = [Cancelling, Completed, Cancelled, Failed],
        [Cancelling] = [Completed, Cancelled, Failed],
    };

    public static bool SyncCanChange(MissionStatus from, MissionStatus to) =>
        SyncTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Vendor job status (§7.1.5) to mission status; null for an unknown code.</summary>
    public static MissionStatus? FromKukaJob(int jobStatus) => jobStatus switch
    {
        10 => Submitted,
        20 or 25 or 50 => InProgress,
        28 => Cancelling,
        30 or 35 => Completed,
        31 => Cancelled,
        60 => Failed,
        _ => null,
    };

    /// <summary>Sets the status, appends the history row and sets <c>FinishedAt</c> for a final status.</summary>
    public static void ChangeStatus(this Mission mission, MissionStatus status, StatusSource source, string? message, DateTimeOffset at)
    {
        mission.StatusHistory.Add(new MissionStatusHistory
        {
            PreviousStatus = mission.Status,
            Status = status,
            Source = source,
            Message = Truncate(message),
            ChangedAt = at,
        });
        mission.Status = status;
        if (Final.Contains(status))
            mission.FinishedAt = at;
    }

    public static string? Truncate(string? message) =>
        message?.Length > MessageMaxLength ? message[..MessageMaxLength] : message;
}
