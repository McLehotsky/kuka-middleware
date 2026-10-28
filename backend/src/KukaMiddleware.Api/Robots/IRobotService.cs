namespace KukaMiddleware.Api.Robots;

public interface IRobotService
{
    Task<IReadOnlyList<RobotSummary>> ListAsync(CancellationToken ct);
}
