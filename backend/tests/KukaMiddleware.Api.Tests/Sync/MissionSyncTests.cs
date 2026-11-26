using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Sync;
using KukaMiddleware.Api.Tests.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace KukaMiddleware.Api.Tests.Sync;

public class MissionSyncTests
{
    private readonly IKukaClient _kuka = Substitute.For<IKukaClient>();
    private readonly ISyncStore _store = Substitute.For<ISyncStore>();
    private readonly TestLogger<MissionSync> _logger = new();
    private readonly List<Mission> _active = [];
    private readonly MissionSync _sync;

    public MissionSyncTests()
    {
        _store.ListActiveMissionsAsync(Arg.Any<CancellationToken>()).Returns(_ => _active.ToList());
        _sync = new MissionSync(_kuka, _store, TestData.Time(), _logger);
    }

    private Mission Active(MissionStatus status, string code = "M-2026-0001")
    {
        var mission = TestData.Mission(status);
        mission.MissionCode = code;
        _active.Add(mission);
        return mission;
    }

    private void Job(string code, int status, string? robotId = "1") =>
        _kuka.QueryJobsAsync(code, Arg.Any<CancellationToken>())
            .Returns(new KukaResult<IReadOnlyList<KukaJob>>([new KukaJob(code, robotId, status)], null));

    private Task Run() => _sync.RunAsync(TestContext.Current.CancellationToken);

    // --- edge cases ---

    [Fact]
    public async Task No_active_missions_calls_nothing()
    {
        await Run();

        await _kuka.DidNotReceiveWithAnyArgs().QueryJobsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Kuka_unreachable_saves_nothing_and_logs()
    {
        var mission = Active(MissionStatus.Submitted);
        _kuka.QueryJobsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new KukaResult<IReadOnlyList<KukaJob>>(null, "KUKA FM is unreachable."));

        await Run();

        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(MissionStatus.Submitted, mission.Status);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("KUKA FM is unreachable."));
    }

    [Fact]
    public async Task Kuka_failure_for_a_later_mission_saves_nothing()
    {
        Active(MissionStatus.Submitted, "M-2026-0001");
        Active(MissionStatus.Submitted, "M-2026-0002");
        Job("M-2026-0001", 20);
        _kuka.QueryJobsAsync("M-2026-0002", Arg.Any<CancellationToken>())
            .Returns(new KukaResult<IReadOnlyList<KukaJob>>(null, "KUKA FM did not respond."));

        await Run();

        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Concurrency_conflict_is_logged_and_retried_next_tick()
    {
        Active(MissionStatus.Submitted);
        Job("M-2026-0001", 20);
        _store.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateConcurrencyException("changed"));

        await Run();

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("next"));
    }

    [Fact]
    public async Task Job_not_found_warns_and_leaves_status_but_sets_last_synced()
    {
        var mission = Active(MissionStatus.Submitted);
        _kuka.QueryJobsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new KukaResult<IReadOnlyList<KukaJob>>([], null));

        await Run();

        Assert.Equal(MissionStatus.Submitted, mission.Status);
        Assert.Equal(TestData.Now, mission.LastSyncedAt);
        Assert.Empty(mission.StatusHistory);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Job_with_another_code_counts_as_not_found()
    {
        var mission = Active(MissionStatus.Submitted);
        _kuka.QueryJobsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new KukaResult<IReadOnlyList<KukaJob>>([new KukaJob("OTHER", "1", 30)], null));

        await Run();

        Assert.Equal(MissionStatus.Submitted, mission.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(40)]
    [InlineData(99)]
    public async Task Unknown_job_status_warns_and_leaves_status_but_sets_last_synced(int jobStatus)
    {
        var mission = Active(MissionStatus.InProgress);
        Job(mission.MissionCode, jobStatus);

        await Run();

        Assert.Equal(MissionStatus.InProgress, mission.Status);
        Assert.Equal(TestData.Now, mission.LastSyncedAt);
        Assert.Empty(mission.StatusHistory);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Theory]
    [InlineData(MissionStatus.Submitted, 10)]
    [InlineData(MissionStatus.InProgress, 20)]
    [InlineData(MissionStatus.InProgress, 25)]
    [InlineData(MissionStatus.InProgress, 50)]
    [InlineData(MissionStatus.Cancelling, 28)]
    public async Task Same_status_writes_no_history_but_sets_last_synced(MissionStatus status, int jobStatus)
    {
        var mission = Active(status);
        Job(mission.MissionCode, jobStatus);

        await Run();

        Assert.Equal(status, mission.Status);
        Assert.Empty(mission.StatusHistory);
        Assert.Equal(TestData.Now, mission.LastSyncedAt);
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MissionStatus.InProgress, 10)]
    [InlineData(MissionStatus.Cancelling, 10)]
    [InlineData(MissionStatus.Cancelling, 20)]
    [InlineData(MissionStatus.Cancelling, 25)]
    [InlineData(MissionStatus.Cancelling, 50)]
    public async Task Transition_outside_the_state_machine_is_ignored(MissionStatus status, int jobStatus)
    {
        var mission = Active(status);
        Job(mission.MissionCode, jobStatus);

        await Run();

        Assert.Equal(status, mission.Status);
        Assert.Empty(mission.StatusHistory);
        Assert.Equal(TestData.Now, mission.LastSyncedAt);
    }

    [Fact]
    public async Task Status_change_logs_a_summary()
    {
        Active(MissionStatus.Submitted);
        Job("M-2026-0001", 20);

        await Run();

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("changed the status of 1"));
    }

    [Fact]
    public async Task No_change_logs_nothing()
    {
        Active(MissionStatus.Submitted);
        Job("M-2026-0001", 10);

        await Run();

        Assert.Empty(_logger.Entries);
    }

    // --- every allowed sync transition (system-definition §5, §14 Phase 5 scope) ---

    [Theory]
    [InlineData(MissionStatus.Submitted, 20, MissionStatus.InProgress)]
    [InlineData(MissionStatus.Submitted, 25, MissionStatus.InProgress)]
    [InlineData(MissionStatus.Submitted, 50, MissionStatus.InProgress)]
    [InlineData(MissionStatus.Submitted, 30, MissionStatus.Completed)]
    [InlineData(MissionStatus.Submitted, 35, MissionStatus.Completed)]
    [InlineData(MissionStatus.Submitted, 31, MissionStatus.Cancelled)]
    [InlineData(MissionStatus.Submitted, 60, MissionStatus.Failed)]
    [InlineData(MissionStatus.Submitted, 28, MissionStatus.Cancelling)]
    [InlineData(MissionStatus.InProgress, 31, MissionStatus.Cancelled)]
    [InlineData(MissionStatus.Cancelling, 60, MissionStatus.Failed)]
    [InlineData(MissionStatus.InProgress, 30, MissionStatus.Completed)]
    [InlineData(MissionStatus.InProgress, 35, MissionStatus.Completed)]
    [InlineData(MissionStatus.InProgress, 60, MissionStatus.Failed)]
    [InlineData(MissionStatus.InProgress, 28, MissionStatus.Cancelling)]
    [InlineData(MissionStatus.Cancelling, 31, MissionStatus.Cancelled)]
    [InlineData(MissionStatus.Cancelling, 30, MissionStatus.Completed)]
    [InlineData(MissionStatus.Cancelling, 35, MissionStatus.Completed)]
    public async Task Status_change_updates_mission_and_appends_history(MissionStatus from, int jobStatus, MissionStatus to)
    {
        var mission = Active(from);
        Job(mission.MissionCode, jobStatus);

        await Run();

        Assert.Equal(to, mission.Status);
        var row = Assert.Single(mission.StatusHistory);
        Assert.Equal(from, row.PreviousStatus);
        Assert.Equal(to, row.Status);
        Assert.Equal(StatusSource.Sync, row.Source);
        Assert.Contains(jobStatus.ToString(), row.Message);
        Assert.Equal(TestData.Now, row.ChangedAt);
    }

    [Theory]
    [InlineData(MissionStatus.Submitted, 30)]
    [InlineData(MissionStatus.Submitted, 31)]
    [InlineData(MissionStatus.Submitted, 60)]
    [InlineData(MissionStatus.InProgress, 35)]
    [InlineData(MissionStatus.Cancelling, 31)]
    [InlineData(MissionStatus.InProgress, 31)]
    [InlineData(MissionStatus.Cancelling, 60)]
    public async Task Final_status_sets_finished_at(MissionStatus from, int jobStatus)
    {
        var mission = Active(from);
        Job(mission.MissionCode, jobStatus);

        await Run();

        Assert.Equal(TestData.Now, mission.FinishedAt);
    }

    [Fact]
    public async Task Non_final_status_does_not_set_finished_at()
    {
        var mission = Active(MissionStatus.Submitted);
        Job(mission.MissionCode, 20);

        await Run();

        Assert.Null(mission.FinishedAt);
    }

    [Fact]
    public async Task Every_processed_mission_gets_last_synced_and_one_save()
    {
        var first = Active(MissionStatus.Submitted, "M-2026-0001");
        var second = Active(MissionStatus.InProgress, "M-2026-0002");
        Job("M-2026-0001", 10);
        Job("M-2026-0002", 30);

        await Run();

        Assert.Equal(TestData.Now, first.LastSyncedAt);
        Assert.Equal(TestData.Now, second.LastSyncedAt);
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // --- robot assignment ---

    [Fact]
    public async Task Mission_without_robot_gets_the_robot_from_the_job()
    {
        var mission = Active(MissionStatus.Submitted);
        Job(mission.MissionCode, 20, "14");
        _store.FindRobotIdAsync("14", Arg.Any<CancellationToken>()).Returns(4);

        await Run();

        Assert.Equal(4, mission.RobotId);
    }

    [Fact]
    public async Task Mission_with_robot_keeps_it()
    {
        var mission = Active(MissionStatus.Submitted);
        mission.RobotId = 2;
        Job(mission.MissionCode, 20, "14");
        _store.FindRobotIdAsync("14", Arg.Any<CancellationToken>()).Returns(4);

        await Run();

        Assert.Equal(2, mission.RobotId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("99")]
    public async Task Unknown_or_missing_job_robot_leaves_robot_empty(string? robotId)
    {
        var mission = Active(MissionStatus.Submitted);
        Job(mission.MissionCode, 20, robotId);

        await Run();

        Assert.Null(mission.RobotId);
    }
}
