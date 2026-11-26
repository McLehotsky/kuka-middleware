using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Sync;

// Tracked data access of the robot and mission sync; one instance (and DbContext) per sync cycle.
public interface ISyncStore
{
    Task<List<Robot>> ListRobotsAsync(CancellationToken ct);
    Task<int?> FindRobotIdAsync(string kukaRobotId, CancellationToken ct);
    void AddRobot(Robot robot);

    /// <summary>Missions in an active status (Submitted, InProgress, Cancelling).</summary>
    Task<List<Mission>> ListActiveMissionsAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
