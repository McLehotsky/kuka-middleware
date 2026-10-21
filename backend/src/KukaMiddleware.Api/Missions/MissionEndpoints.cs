namespace KukaMiddleware.Api.Missions;

public static class MissionEndpoints
{
    public static void MapMissionEndpoints(this IEndpointRouteBuilder app)
    {
        var missions = app.MapGroup("/api/missions");

        missions.MapPost("/", async (CreateMissionRequest request, IMissionService service, CancellationToken ct) =>
            await service.CreateAsync(request, ct) switch
            {
                CreateMissionResult.Created c => Results.Created($"/api/missions/{c.Mission.Id}", c.Mission),
                CreateMissionResult.Invalid i => Results.ValidationProblem(i.Errors),
                _ => throw new InvalidOperationException(),
            });

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

        missions.MapPost("/{id:guid}/cancel", async (Guid id, IMissionService service, CancellationToken ct) =>
            await service.CancelAsync(id, ct) switch
            {
                CancelMissionResult.Cancelled c => Results.Ok(c.Mission),
                CancelMissionResult.Conflict c => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Mission cannot be cancelled",
                    detail: $"A mission in status {c.Status} cannot be cancelled."),
                _ => MissionNotFound(),
            });
    }

    private static IResult MissionNotFound() =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Mission not found");
}
