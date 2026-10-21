using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Missions;

public class ListMissionsTests
{
    private readonly IMissionStore _store = Substitute.For<IMissionStore>();
    private readonly MissionService _service;
    private IReadOnlyCollection<MissionStatus>? _requested;

    public ListMissionsTests()
    {
        _store.ListAsync(Arg.Do<IReadOnlyCollection<MissionStatus>?>(s => _requested = s), Arg.Any<CancellationToken>())
            .Returns([]);
        _service = new MissionService(_store, TestData.Time());
    }

    private Task<IReadOnlyList<MissionSummary>?> List(string? state) =>
        _service.ListAsync(state, TestContext.Current.CancellationToken);

    private void StoreReturns(params Mission[] missions) =>
        _store.ListAsync(Arg.Any<IReadOnlyCollection<MissionStatus>?>(), Arg.Any<CancellationToken>()).Returns([.. missions]);

    [Fact]
    public async Task Active_filter_requests_every_not_final_status_including_created()
    {
        await List("active");

        Assert.Equal(
            [MissionStatus.Created, MissionStatus.Submitted, MissionStatus.InProgress, MissionStatus.Cancelling],
            _requested!.Order());
    }

    [Fact]
    public async Task Finished_filter_requests_final_statuses()
    {
        await List("finished");

        Assert.Equal([MissionStatus.Completed, MissionStatus.Cancelled, MissionStatus.Failed], _requested!.Order());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task No_state_requests_all_missions(string? state)
    {
        var result = await List(state);

        Assert.NotNull(result);
        Assert.Null(_requested);
    }

    [Theory]
    [InlineData("running")]
    [InlineData("all")]
    [InlineData("Active")]
    public async Task Invalid_state_returns_null_without_reading_the_store(string state)
    {
        Assert.Null(await List(state));
        await _store.DidNotReceive().ListAsync(Arg.Any<IReadOnlyCollection<MissionStatus>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missions_are_ordered_by_created_at_descending()
    {
        var oldest = TestData.Mission(createdAt: TestData.Now.AddDays(-2));
        var newest = TestData.Mission(createdAt: TestData.Now);
        var middle = TestData.Mission(createdAt: TestData.Now.AddDays(-1));
        StoreReturns(oldest, newest, middle);

        var result = await List(null);

        Assert.Equal([newest.Id, middle.Id, oldest.Id], result!.Select(m => m.Id));
    }

    [Fact]
    public async Task Summary_contains_robot_name()
    {
        var mission = TestData.Mission();
        mission.Robot = new Robot { KukaRobotId = "R1", Name = "AMR-01" };
        StoreReturns(mission);

        var result = await List(null);

        Assert.Equal("AMR-01", Assert.Single(result!).RobotName);
    }
}
