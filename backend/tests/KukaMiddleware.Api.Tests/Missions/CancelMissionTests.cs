using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Missions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace KukaMiddleware.Api.Tests.Missions;

public class CancelMissionTests
{
    private readonly IMissionStore _store = Substitute.For<IMissionStore>();
    private readonly IKukaClient _kuka = TestData.Kuka();
    private readonly MissionService _service;

    public CancelMissionTests() => _service = new MissionService(_store, _kuka, TestData.Time(), TestData.Client());

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

    [Fact]
    public async Task Unknown_mission_does_not_call_kuka()
    {
        await Cancel(Guid.NewGuid());

        await _kuka.DidNotReceiveWithAnyArgs().CancelMissionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MissionStatus.Completed)]
    [InlineData(MissionStatus.Cancelled)]
    [InlineData(MissionStatus.Failed)]
    [InlineData(MissionStatus.Cancelling)]
    public async Task Conflict_does_not_call_kuka(MissionStatus status)
    {
        await Cancel(Stored(status).Id);

        await _kuka.DidNotReceiveWithAnyArgs().CancelMissionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_calls_kuka_with_the_mission_code()
    {
        var mission = Stored(MissionStatus.InProgress);

        await Cancel(mission.Id);

        await _kuka.Received(1).CancelMissionAsync(mission.MissionCode, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Kuka_failure_returns_the_error()
    {
        var mission = Stored(MissionStatus.InProgress);
        _kuka.CancelMissionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new KukaResult("KUKA FM returned HTTP 500."));

        var failed = Assert.IsType<CancelMissionResult.KukaFailed>(await Cancel(mission.Id));

        Assert.Equal("KUKA FM returned HTTP 500.", failed.Error);
    }

    [Theory]
    [InlineData(MissionStatus.Created)]
    [InlineData(MissionStatus.Submitted)]
    [InlineData(MissionStatus.InProgress)]
    public async Task Kuka_failure_leaves_the_mission_unchanged(MissionStatus status)
    {
        var mission = Stored(status);
        _kuka.CancelMissionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new KukaResult("KUKA FM is unreachable."));

        await Cancel(mission.Id);

        Assert.Equal(status, mission.Status);
        Assert.Empty(mission.StatusHistory);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // The reload stub mimics the store: unsaved history is dropped and the stored values come back.
    private void ConflictOnFirstSave(Mission mission, MissionStatus storedStatus)
    {
        var saves = 0;
        _store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            ++saves == 1 ? Task.FromException(new DbUpdateConcurrencyException("changed")) : Task.CompletedTask);
        _store.ReloadAsync(mission, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
        {
            mission.StatusHistory.Clear();
            mission.Status = storedStatus;
            mission.LastSyncedAt = TestData.Now;
        });
    }

    [Fact]
    public async Task Conflict_from_a_sync_timestamp_is_retried_once_and_cancels()
    {
        var mission = Stored(MissionStatus.InProgress);
        ConflictOnFirstSave(mission, MissionStatus.InProgress);

        Assert.IsType<CancelMissionResult.Cancelled>(await Cancel(mission.Id));

        Assert.Equal(MissionStatus.Cancelling, mission.Status);
        var row = Assert.Single(mission.StatusHistory);
        Assert.Equal(MissionStatus.InProgress, row.PreviousStatus);
        Assert.Equal(MissionStatus.Cancelling, row.Status);
        await _store.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MissionStatus.Cancelling)]
    [InlineData(MissionStatus.Completed)]
    [InlineData(MissionStatus.Failed)]
    public async Task Conflict_with_a_status_that_is_no_longer_cancellable_is_a_conflict(MissionStatus storedStatus)
    {
        var mission = Stored(MissionStatus.InProgress);
        ConflictOnFirstSave(mission, storedStatus);

        Assert.IsType<CancelMissionResult.ConcurrentUpdate>(await Cancel(mission.Id));

        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Second_conflict_is_a_conflict()
    {
        var mission = Stored(MissionStatus.InProgress);
        _store.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateConcurrencyException("changed"));
        _store.ReloadAsync(mission, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask).AndDoes(_ =>
        {
            mission.StatusHistory.Clear();
            mission.Status = MissionStatus.InProgress;
        });

        Assert.IsType<CancelMissionResult.ConcurrentUpdate>(await Cancel(mission.Id));

        await _store.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Kuka_cancel_and_save_ignore_caller_cancellation()
    {
        var mission = Stored(MissionStatus.InProgress);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await _service.CancelAsync(mission.Id, cts.Token);

        await _kuka.Received(1).CancelMissionAsync(mission.MissionCode, Arg.Is<CancellationToken>(t => !t.CanBeCanceled));
        await _store.Received(1).SaveChangesAsync(Arg.Is<CancellationToken>(t => !t.CanBeCanceled));
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

    [Theory]
    [InlineData(MissionStatus.Created)]
    [InlineData(MissionStatus.Submitted)]
    [InlineData(MissionStatus.InProgress)]
    public async Task Cancel_preserves_existing_history(MissionStatus status)
    {
        var mission = Stored(status);
        var original = new MissionStatusHistory
        {
            Id = 42,
            MissionId = mission.Id,
            Status = status,
            Source = StatusSource.Api,
            Message = "existing history",
            ChangedAt = TestData.Now.AddMinutes(-10),
        };
        mission.StatusHistory.Add(original);

        await Cancel(mission.Id);

        Assert.Equal(2, mission.StatusHistory.Count);
        var preserved = Assert.Single(mission.StatusHistory, row => row.Id == 42);
        Assert.Equal(mission.Id, preserved.MissionId);
        Assert.Null(preserved.PreviousStatus);
        Assert.Equal(status, preserved.Status);
        Assert.Equal(StatusSource.Api, preserved.Source);
        Assert.Equal("existing history", preserved.Message);
        Assert.Equal(TestData.Now.AddMinutes(-10), preserved.ChangedAt);
        var added = Assert.Single(mission.StatusHistory, row => row.Id != 42);
        Assert.Equal(status, added.PreviousStatus);
        Assert.Equal(MissionStatus.Cancelling, added.Status);
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
