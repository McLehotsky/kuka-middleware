using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Missions;

public interface IMissionStore
{
    Task<long> NextMissionCodeNumberAsync(CancellationToken ct);
    Task<Robot?> FindRobotAsync(int robotId, CancellationToken ct);
    Task<bool> MissionExistsAsync(Guid id, CancellationToken ct);

    /// <summary>Tracked mission with robot, creating client and steps.</summary>
    Task<Mission?> FindAsync(Guid id, CancellationToken ct);

    /// <summary>Missions with robot; <paramref name="statuses"/> null = all.</summary>
    Task<List<Mission>> ListAsync(IReadOnlyCollection<MissionStatus>? statuses, CancellationToken ct);

    Task<List<MissionStatusHistory>> ListHistoryAsync(Guid missionId, CancellationToken ct);
    void Add(Mission mission);

    /// <summary>Discards unsaved status history of the mission and reloads its stored values.</summary>
    Task ReloadAsync(Mission mission, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
