namespace KukaMiddleware.Api.Robots;

public static class RobotEndpoints
{
    public static void MapRobotEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/robots", async (IRobotService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));
}
