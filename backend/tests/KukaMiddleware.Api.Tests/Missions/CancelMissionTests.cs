using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Missions;

public class CancelMissionTests
{
    private readonly IMissionStore _store = Substitute.For<IMissionStore>();
    private readonly MissionService _service;

    public CancelMissionTests() => _service = new MissionService(_store, TestData.Time());

    private Mission Stored(MissionStatus status)
    {
        var mission = TestData.Mission(status);
        _store.FindAsync(mission.Id, Arg.Any<CancellationToken>()).Returns(mission);
        return mission;
    }

    private Task<CancelMissionResult> Cancel(Guid id) => _service.CancelAsync(id, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Unknown_mission_is_not_found()
    {
        Assert.IsType<CancelMissionResult.NotFound>(await Cancel(Guid.NewGuid()));
    }

    [Fact]
    public async Task Unknown_mission_saves_nothing()
    {
        await Cancel(Guid.NewGuid());

        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MissionStatus.Completed)]
    [InlineData(MissionStatus.Cancelled)]
    [InlineData(MissionStatus.Failed)]
    [InlineData(MissionStatus.Cancelling)]
    public async Task Final_or_cancelling_mission_is_a_conflict(MissionStatus status)
    {
        var mission = Stored(status);

        var conflict = Assert.IsType<CancelMissionResult.Conflict>(await Cancel(mission.Id));

        Assert.Equal(status, conflict.Status);
    }

    [Theory]
    [InlineData(MissionStatus.Completed)]
    [InlineData(MissionStatus.Cancelling)]
    public async Task Conflict_changes_nothing(MissionStatus status)
    {
        var mission = Stored(status);

        await Cancel(mission.Id);

        Assert.Equal(status, mission.Status);
        Assert.Empty(mission.StatusHistory);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MissionStatus.Created)]
    [InlineData(MissionStatus.Submitted)]
    [InlineData(MissionStatus.InProgress)]
    public async Task Cancellable_mission_becomes_cancelling(MissionStatus status)
    {
        var mission = Stored(status);

        await Cancel(mission.Id);

        Assert.Equal(MissionStatus.Cancelling, mission.Status);
    }

    [Theory]
    [InlineData(MissionStatus.Created)]
    [InlineData(MissionStatus.Submitted)]
    [InlineData(MissionStatus.InProgress)]
    public async Task Cancel_writes_history_row(MissionStatus status)
    {
        var mission = Stored(status);

        await Cancel(mission.Id);

        var row = Assert.Single(mission.StatusHistory);
        Assert.Equal(status, row.PreviousStatus);
        Assert.Equal(MissionStatus.Cancelling, row.Status);
        Assert.Equal(StatusSource.Api, row.Source);
        Assert.Equal("cancel requested", row.Message);
        Assert.Equal(TestData.Now, row.ChangedAt);
    }

    [Fact]
    public async Task Cancel_saves_changes()
    {
        var mission = Stored(MissionStatus.Created);

        await Cancel(mission.Id);

        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_returns_summary_with_cancelling_status()
    {
        var mission = Stored(MissionStatus.InProgress);

        var cancelled = Assert.IsType<CancelMissionResult.Cancelled>(await Cancel(mission.Id));

        Assert.Equal(mission.Id, cancelled.Mission.Id);
        Assert.Equal(MissionStatus.Cancelling, cancelled.Mission.Status);
    }
}
