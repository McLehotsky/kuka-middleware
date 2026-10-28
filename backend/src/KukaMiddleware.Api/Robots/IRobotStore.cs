using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Robots;

public interface IRobotStore
{
    Task<List<Robot>> ListAsync(CancellationToken ct);

    /// <summary>Non-final missions that have a robot assigned.</summary>
    Task<List<Mission>> ListActiveMissionsAsync(CancellationToken ct);
}
