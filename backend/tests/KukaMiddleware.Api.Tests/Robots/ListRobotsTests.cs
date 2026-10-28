using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Robots;
using KukaMiddleware.Api.Tests.Missions;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Robots;

public class ListRobotsTests
{
    private readonly IRobotStore _store = Substitute.For<IRobotStore>();
    private readonly RobotService _service;

    public ListRobotsTests()
    {
        _store.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        _store.ListActiveMissionsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _service = new RobotService(_store);
    }

    private Task<IReadOnlyList<RobotSummary>> List() => _service.ListAsync(TestContext.Current.CancellationToken);

    private void StoreReturns(params Robot[] robots) => _store.ListAsync(Arg.Any<CancellationToken>()).Returns([.. robots]);

    private void MissionsReturn(params Mission[] missions) =>
        _store.ListActiveMissionsAsync(Arg.Any<CancellationToken>()).Returns([.. missions]);

    private static Robot Robot(int id = 1, string kukaRobotId = "R1", string? name = "AMR-01") => new()
    {
        Id = id,
        KukaRobotId = kukaRobotId,
        Name = name,
        Status = RobotStatus.Available,
        UpdatedAt = TestData.Now,
    };

    private static Mission MissionOf(Robot robot, MissionStatus status, DateTimeOffset createdAt, string code)
    {
        var mission = TestData.Mission(status, createdAt);
        mission.MissionCode = code;
        mission.RobotId = robot.Id;
        return mission;
    }

    [Fact]
    public async Task Empty_table_returns_empty_list()
    {
        Assert.Empty(await List());
    }

    [Theory]
    [InlineData(RobotStatus.Available)]
    [InlineData(RobotStatus.Busy)]
    [InlineData(RobotStatus.Offline)]
    public async Task Robot_status_is_returned(RobotStatus status)
    {
        var robot = Robot();
        robot.Status = status;
        StoreReturns(robot);

        Assert.Equal(status, Assert.Single(await List()).Status);
    }

    [Fact]
    public async Task Robot_fields_are_mapped()
    {
        var robot = Robot(id: 7, kukaRobotId: "KR-7", name: "AMR-07");
        robot.BatteryLevel = 85;
        robot.CurrentNode = "NODE-12";
        robot.LastSeenAt = TestData.Now.AddMinutes(-1);
        StoreReturns(robot);

        var summary = Assert.Single(await List());

        Assert.Equal(
            new RobotSummary(7, "KR-7", "AMR-07", RobotStatus.Available, 85, "NODE-12", null, TestData.Now.AddMinutes(-1), TestData.Now),
            summary);
    }

    [Fact]
    public async Task Missing_optional_values_map_to_null()
    {
        StoreReturns(Robot(name: null));

        var summary = Assert.Single(await List());

        Assert.Null(summary.Name);
        Assert.Null(summary.BatteryLevel);
        Assert.Null(summary.CurrentNode);
        Assert.Null(summary.LastSeenAt);
        Assert.Null(summary.CurrentMission);
    }

    [Fact]
    public async Task Robots_are_ordered_by_name_falling_back_to_kuka_robot_id()
    {
        StoreReturns(
            Robot(id: 1, kukaRobotId: "Z-1", name: "Charlie"),
            Robot(id: 2, kukaRobotId: "Bravo-KR", name: null),
            Robot(id: 3, kukaRobotId: "A-3", name: "Alpha"),
            Robot(id: 4, kukaRobotId: "D-4", name: "Delta"));

        var result = await List();

        Assert.Equal([3, 2, 1, 4], result.Select(r => r.Id));
    }

    [Fact]
    public async Task No_missions_gives_no_current_mission()
    {
        var robot = Robot();
        StoreReturns(robot, Robot(id: 2, kukaRobotId: "R2"));
        MissionsReturn(MissionOf(Robot(id: 2), MissionStatus.InProgress, TestData.Now, "M-2026-0002"));

        Assert.Null((await List()).Single(r => r.Id == 1).CurrentMission);
    }

    [Theory]
    [InlineData(MissionStatus.Created)]
    [InlineData(MissionStatus.Submitted)]
    [InlineData(MissionStatus.InProgress)]
    [InlineData(MissionStatus.Cancelling)]
    public async Task One_non_final_mission_is_the_current_mission(MissionStatus status)
    {
        var robot = Robot();
        var mission = MissionOf(robot, status, TestData.Now, "M-2026-0001");
        StoreReturns(robot);
        MissionsReturn(mission);

        Assert.Equal(new RobotSummary.MissionRef(mission.Id, "M-2026-0001"), Assert.Single(await List()).CurrentMission);
    }

    [Theory]
    [InlineData(MissionStatus.Completed)]
    [InlineData(MissionStatus.Cancelled)]
    [InlineData(MissionStatus.Failed)]
    public async Task Final_missions_are_never_the_current_mission(MissionStatus status)
    {
        var robot = Robot();
        StoreReturns(robot);
        MissionsReturn(MissionOf(robot, status, TestData.Now, "M-2026-0001"));

        Assert.Null(Assert.Single(await List()).CurrentMission);
    }

    [Fact]
    public async Task Newest_non_final_mission_is_the_current_mission()
    {
        var robot = Robot();
        StoreReturns(robot);
        MissionsReturn(
            MissionOf(robot, MissionStatus.InProgress, TestData.Now.AddHours(-2), "M-2026-0001"),
            MissionOf(robot, MissionStatus.Submitted, TestData.Now, "M-2026-0003"),
            MissionOf(robot, MissionStatus.Completed, TestData.Now.AddHours(1), "M-2026-0004"),
            MissionOf(robot, MissionStatus.Created, TestData.Now.AddHours(-1), "M-2026-0002"));

        Assert.Equal("M-2026-0003", Assert.Single(await List()).CurrentMission?.MissionCode);
    }
}
