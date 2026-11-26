using KukaMiddleware.Api.Audit;
using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KukaMiddleware.Api.Tests.Missions;

// Mapping of service results to HTTP responses (system-definition §8).
public class MissionEndpointResultTests
{
    private readonly DefaultHttpContext _http = new();
    private static readonly Guid Id = Guid.NewGuid();

    private static MissionSummary Summary() =>
        new(Id, "M-2026-0001", null, "RACK_MOVE", 2, MissionStatus.Cancelling, null, TestData.Now, null);

    [Fact]
    public void Kuka_failure_on_dispatch_is_502_with_the_mission_id()
    {
        var result = MissionEndpoints.ToHttp(new CreateMissionResult.KukaFailed(Id, "KUKA FM is unreachable."), _http);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Equal(Id, problem.ProblemDetails.Extensions["missionId"]);
        Assert.Equal("KUKA FM is unreachable.", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Created_mission_is_201_with_location()
    {
        var result = MissionEndpoints.ToHttp(new CreateMissionResult.Created(new CreateMissionResponse(Id, "M-2026-0001")), _http);

        var created = Assert.IsType<Created<CreateMissionResponse>>(result);
        Assert.Equal($"/api/missions/{Id}", created.Location);
    }

    [Theory]
    [MemberData(nameof(DispatchedResults))]
    public void Dispatched_mission_is_linked_for_the_audit(CreateMissionResult serviceResult)
    {
        MissionEndpoints.ToHttp(serviceResult, _http);

        Assert.Equal(Id, _http.Items[AuditMiddleware.MissionIdItem]);
    }

    public static TheoryData<CreateMissionResult> DispatchedResults =>
    [
        new CreateMissionResult.Created(new CreateMissionResponse(Id, "M-2026-0001")),
        new CreateMissionResult.KukaFailed(Id, "error"),
    ];

    [Fact]
    public void Invalid_dispatch_links_no_mission()
    {
        MissionEndpoints.ToHttp(new CreateMissionResult.Invalid(new() { ["steps"] = ["required"] }), _http);

        Assert.False(_http.Items.ContainsKey(AuditMiddleware.MissionIdItem));
    }

    [Fact]
    public void Kuka_failure_on_cancel_is_502()
    {
        var result = MissionEndpoints.ToHttp(Id, new CancelMissionResult.KukaFailed("KUKA FM returned HTTP 500."), _http);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, problem.StatusCode);
        Assert.Equal("KUKA FM returned HTTP 500.", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Concurrent_cancel_is_409()
    {
        var result = MissionEndpoints.ToHttp(Id, new CancelMissionResult.ConcurrentUpdate(), _http);

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ProblemHttpResult>(result).StatusCode);
    }

    [Theory]
    [MemberData(nameof(ExistingMissionResults))]
    public void Cancel_of_an_existing_mission_is_linked_for_the_audit(CancelMissionResult serviceResult)
    {
        MissionEndpoints.ToHttp(Id, serviceResult, _http);

        Assert.Equal(Id, _http.Items[AuditMiddleware.MissionIdItem]);
    }

    public static TheoryData<CancelMissionResult> ExistingMissionResults =>
    [
        new CancelMissionResult.Cancelled(Summary()),
        new CancelMissionResult.Conflict(MissionStatus.Completed),
        new CancelMissionResult.ConcurrentUpdate(),
        new CancelMissionResult.KukaFailed("error"),
    ];

    [Fact]
    public void Cancel_of_an_unknown_mission_is_404_and_not_linked()
    {
        var result = MissionEndpoints.ToHttp(Id, new CancelMissionResult.NotFound(), _http);

        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ProblemHttpResult>(result).StatusCode);
        Assert.False(_http.Items.ContainsKey(AuditMiddleware.MissionIdItem));
    }
}
