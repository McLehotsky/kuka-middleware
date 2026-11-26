using KukaMiddleware.Api.Audit;

namespace KukaMiddleware.Api.Missions;

public static class MissionEndpoints
{
    public static void MapMissionEndpoints(this IEndpointRouteBuilder app)
    {
        var missions = app.MapGroup("/api/missions");

        missions.MapPost("/", async (CreateMissionRequest request, IMissionService service, HttpContext http, CancellationToken ct) =>
            ToHttp(await service.CreateAsync(request, ct), http));

        missions.MapGet("/", async (string? state, IMissionService service, CancellationToken ct) =>
            await service.ListAsync(state, ct) is { } list
                ? Results.Ok(list)
                : Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["state"] = ["Must be 'active' or 'finished', or omitted for all missions."],
                }));

        missions.MapGet("/{id:guid}", async (Guid id, IMissionService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } detail ? Results.Ok(detail) : MissionNotFound());

        missions.MapGet("/{id:guid}/history", async (Guid id, IMissionService service, CancellationToken ct) =>
            await service.GetHistoryAsync(id, ct) is { } history ? Results.Ok(history) : MissionNotFound());

        missions.MapPost("/{id:guid}/cancel", async (Guid id, IMissionService service, HttpContext http, CancellationToken ct) =>
            ToHttp(id, await service.CancelAsync(id, ct), http));
    }

    public static IResult ToHttp(CreateMissionResult result, HttpContext http)
    {
        switch (result)
        {
            case CreateMissionResult.Created c:
                AuditMiddleware.LinkMission(http, c.Mission.Id);
                return Results.Created($"/api/missions/{c.Mission.Id}", c.Mission);
            case CreateMissionResult.KukaFailed f:
                AuditMiddleware.LinkMission(http, f.MissionId);
                return Results.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "KUKA FM did not accept the mission",
                    detail: f.Error,
                    extensions: new Dictionary<string, object?> { ["missionId"] = f.MissionId });
            case CreateMissionResult.Invalid i:
                return Results.ValidationProblem(i.Errors);
            default:
                throw new InvalidOperationException();
        }
    }

    public static IResult ToHttp(Guid id, CancelMissionResult result, HttpContext http)
    {
        if (result is not CancelMissionResult.NotFound)
            AuditMiddleware.LinkMission(http, id);

        return result switch
        {
            CancelMissionResult.Cancelled c => Results.Ok(c.Mission),
            CancelMissionResult.Conflict c => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Mission cannot be cancelled",
                detail: $"A mission in status {c.Status} cannot be cancelled."),
            CancelMissionResult.ConcurrentUpdate => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Mission cannot be cancelled",
                detail: "KUKA FM accepted the cancel, but the mission changed meanwhile. Reload it to see its current status."),
            CancelMissionResult.KukaFailed f => Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "KUKA FM did not cancel the mission",
                detail: f.Error),
            _ => MissionNotFound(),
        };
    }

    private static IResult MissionNotFound() =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Mission not found");
}
