using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Sync;
using KukaMiddleware.Api.Tests.Missions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Sync;

public class RobotSyncTests
{
    private readonly IKukaClient _kuka = Substitute.For<IKukaClient>();
    private readonly ISyncStore _store = Substitute.For<ISyncStore>();
    private readonly TestLogger<RobotSync> _logger = new();
    private readonly List<Robot> _stored = [];
    private readonly List<Robot> _added = [];
    private readonly RobotSync _sync;

    public RobotSyncTests()
    {
        _store.ListRobotsAsync(Arg.Any<CancellationToken>()).Returns(_ => _stored.ToList());
        _store.AddRobot(Arg.Do<Robot>(_added.Add));
        _sync = new RobotSync(_kuka, _store, TestData.Time(), _logger);
    }

    private void KukaReturns(params KukaRobot[] robots) =>
        _kuka.QueryRobotsAsync(Arg.Any<CancellationToken>()).Returns(new KukaResult<IReadOnlyList<KukaRobot>>(robots, null));

    private Robot Existing(string kukaId, RobotStatus status = RobotStatus.Available)
    {
        var robot = new Robot
        {
            Id = _stored.Count + 1,
            KukaRobotId = kukaId,
            Status = status,
            BatteryLevel = 50,
            CurrentNode = "OLD",
            LastSeenAt = TestData.Now.AddHours(-1),
            UpdatedAt = TestData.Now.AddHours(-1),
        };
        _stored.Add(robot);
        return robot;
    }

    private Task Run() => _sync.RunAsync(TestContext.Current.CancellationToken);

    // --- edge cases ---

    [Fact]
    public async Task Failed_call_leaves_data_unchanged()
    {
        Existing("1");
        _kuka.QueryRobotsAsync(Arg.Any<CancellationToken>())
            .Returns(new KukaResult<IReadOnlyList<KukaRobot>>(null, "KUKA FM is unreachable."));

        await Run();

        _store.DidNotReceiveWithAnyArgs().AddRobot(default!);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("OLD", _stored[0].CurrentNode);
    }

    [Fact]
    public async Task Failed_call_is_logged()
    {
        _kuka.QueryRobotsAsync(Arg.Any<CancellationToken>())
            .Returns(new KukaResult<IReadOnlyList<KukaRobot>>(null, "KUKA FM is unreachable."));

        await Run();

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("KUKA FM is unreachable."));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(99)]
    public async Task Unknown_kuka_status_leaves_the_robot_unchanged_and_warns(int kukaStatus)
    {
        var robot = Existing("1", RobotStatus.Busy);
        KukaReturns(new KukaRobot("1", kukaStatus, 0.9, "N-9"));

        await Run();

        Assert.Equal(RobotStatus.Busy, robot.Status);
        Assert.Equal("OLD", robot.CurrentNode);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Unknown_kuka_status_of_a_new_robot_inserts_nothing()
    {
        KukaReturns(new KukaRobot("1", 99, 0.9, "N-9"));

        await Run();

        _store.DidNotReceiveWithAnyArgs().AddRobot(default!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Robot_without_id_is_skipped(string? kukaId)
    {
        KukaReturns(new KukaRobot(kukaId!, 3, 0.9, "N-9"));

        await Run();

        _store.DidNotReceiveWithAnyArgs().AddRobot(default!);
    }

    [Fact]
    public async Task Robot_missing_from_the_response_stays_unchanged()
    {
        var missing = Existing("2", RobotStatus.Busy);
        KukaReturns(new KukaRobot("1", 3, 0.9, "N-9"));

        await Run();

        Assert.Equal(RobotStatus.Busy, missing.Status);
        Assert.Equal(TestData.Now.AddHours(-1), missing.UpdatedAt);
    }

    // --- status mapping (system-definition §14 Phase 5 scope) ---

    [Theory]
    [InlineData(3, RobotStatus.Available)]
    [InlineData(1, RobotStatus.Busy)]
    [InlineData(4, RobotStatus.Busy)]
    [InlineData(5, RobotStatus.Busy)]
    [InlineData(6, RobotStatus.Busy)]
    [InlineData(2, RobotStatus.Offline)]
    [InlineData(7, RobotStatus.Offline)]
    public async Task Kuka_status_is_mapped(int kukaStatus, RobotStatus expected)
    {
        var robot = Existing("1");
        KukaReturns(new KukaRobot("1", kukaStatus, 0.9, "N-9"));

        await Run();

        Assert.Equal(expected, robot.Status);
    }

    // --- insert / update ---

    [Fact]
    public async Task New_robot_is_inserted()
    {
        KukaReturns(new KukaRobot("1", 3, 0.75, "Labak-CHTN-15"));

        await Run();

        var robot = Assert.Single(_added);
        Assert.Equal("1", robot.KukaRobotId);
        Assert.Null(robot.Name);
        Assert.Equal(RobotStatus.Available, robot.Status);
        Assert.Equal(75, robot.BatteryLevel);
        Assert.Equal("Labak-CHTN-15", robot.CurrentNode);
        Assert.Equal(TestData.Now, robot.LastSeenAt);
        Assert.Equal(TestData.Now, robot.UpdatedAt);
    }

    [Fact]
    public async Task Existing_robot_is_updated_not_inserted()
    {
        var robot = Existing("1", RobotStatus.Available);
        KukaReturns(new KukaRobot("1", 4, 0.4, "N-9"));

        await Run();

        _store.DidNotReceiveWithAnyArgs().AddRobot(default!);
        Assert.Equal(RobotStatus.Busy, robot.Status);
        Assert.Equal(40, robot.BatteryLevel);
        Assert.Equal("N-9", robot.CurrentNode);
        Assert.Equal(TestData.Now, robot.UpdatedAt);
        Assert.Equal(TestData.Now, robot.LastSeenAt);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    public async Task Offline_robot_keeps_its_last_seen_time(int kukaStatus)
    {
        var robot = Existing("1");
        KukaReturns(new KukaRobot("1", kukaStatus, 0.4, "N-9"));

        await Run();

        Assert.Equal(TestData.Now.AddHours(-1), robot.LastSeenAt);
        Assert.Equal(TestData.Now, robot.UpdatedAt);
    }

    [Fact]
    public async Task Offline_robot_without_battery_and_node_keeps_the_last_known_values()
    {
        var robot = Existing("1");
        KukaReturns(new KukaRobot("1", 2, null, null));

        await Run();

        Assert.Equal(50, robot.BatteryLevel);
        Assert.Equal("OLD", robot.CurrentNode);
    }

    [Fact]
    public async Task Online_robot_without_battery_and_node_clears_them()
    {
        var robot = Existing("1");
        KukaReturns(new KukaRobot("1", 3, null, null));

        await Run();

        Assert.Null(robot.BatteryLevel);
        Assert.Null(robot.CurrentNode);
    }

    [Fact]
    public async Task New_offline_robot_has_no_last_seen_time()
    {
        KukaReturns(new KukaRobot("1", 2, 0.4, "N-9"));

        await Run();

        Assert.Null(Assert.Single(_added).LastSeenAt);
    }

    [Theory]
    [InlineData(1.0, 100)]
    [InlineData(0.5, 50)]
    [InlineData(0.0, 0)]
    [InlineData(0.456, 46)]
    [InlineData(87.0, 87)]
    [InlineData(100.0, 100)]
    [InlineData(150.0, 100)]
    [InlineData(-1.0, 0)]
    [InlineData(null, null)]
    public async Task Battery_level_is_stored_as_percent(double? kukaLevel, int? percent)
    {
        KukaReturns(new KukaRobot("1", 3, kukaLevel, "N-9"));

        await Run();

        Assert.Equal(percent, Assert.Single(_added).BatteryLevel);
    }

    [Fact]
    public async Task New_robot_logs_a_summary()
    {
        KukaReturns(new KukaRobot("1", 3, 0.9, "N-1"));

        await Run();

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("status of 1 robots"));
    }

    [Fact]
    public async Task Status_change_of_an_existing_robot_logs_a_summary()
    {
        Existing("1", RobotStatus.Available);
        KukaReturns(new KukaRobot("1", 4, 0.9, "N-1"));

        await Run();

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information);
    }

    [Fact]
    public async Task Unchanged_status_logs_nothing()
    {
        Existing("1", RobotStatus.Available);
        KukaReturns(new KukaRobot("1", 3, 0.8, "N-2"));

        await Run();

        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task Changes_are_saved_once()
    {
        Existing("1");
        KukaReturns(new KukaRobot("1", 3, 0.9, "N-1"), new KukaRobot("2", 4, 0.8, "N-2"));

        await Run();

        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
