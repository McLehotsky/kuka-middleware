using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Missions;

public class CreateMissionTests
{
    private static readonly CreateMissionRequest Request =
        new("RACK_MOVE", null, null, "ERP-1", [new("N-01", "Load"), new("N-02", "Wait"), new("N-03", "Unload")]);

    private readonly IMissionStore _store = Substitute.For<IMissionStore>();
    private readonly MissionService _service;
    private Mission? _added;

    public CreateMissionTests()
    {
        _store.NextMissionCodeNumberAsync(Arg.Any<CancellationToken>()).Returns(42L);
        _store.Add(Arg.Do<Mission>(m => _added = m));
        _service = new MissionService(_store, TestData.Time());
    }

    private Task<CreateMissionResult> Create(CreateMissionRequest request) =>
        _service.CreateAsync(request, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Returns_id_and_code_of_the_stored_mission()
    {
        var created = Assert.IsType<CreateMissionResult.Created>(await Create(Request));

        Assert.Equal(_added!.Id, created.Mission.Id);
        Assert.Equal(_added.MissionCode, created.Mission.MissionCode);
    }

    [Theory]
    [InlineData(1L, "M-2026-0001")]
    [InlineData(42L, "M-2026-0042")]
    [InlineData(12345L, "M-2026-12345")]
    public async Task Mission_code_uses_year_from_time_provider_and_sequence_number(long number, string expected)
    {
        _store.NextMissionCodeNumberAsync(Arg.Any<CancellationToken>()).Returns(number);

        await Create(Request);

        Assert.Equal(expected, _added!.MissionCode);
    }

    [Fact]
    public async Task Stores_mission_with_status_created()
    {
        await Create(Request);

        Assert.Equal(MissionStatus.Created, _added!.Status);
    }

    [Fact]
    public async Task Stores_created_at_from_time_provider()
    {
        await Create(Request);

        Assert.Equal(TestData.Now, _added!.CreatedAt);
    }

    [Fact]
    public async Task Attributes_mission_to_seeded_web_ui_client()
    {
        await Create(Request);

        Assert.Equal(1, _added!.CreatedByClientId);
    }

    [Fact]
    public async Task Priority_defaults_to_2_when_omitted()
    {
        await Create(Request);

        Assert.Equal(2, _added!.Priority);
    }

    [Fact]
    public async Task Stores_given_priority()
    {
        await Create(Request with { Priority = 1 });

        Assert.Equal(1, _added!.Priority);
    }

    [Fact]
    public async Task Stores_steps_with_sequence_1_to_n_in_request_order()
    {
        await Create(Request);

        Assert.Equal(
            [(1, "N-01", "Load"), (2, "N-02", "Wait"), (3, "N-03", "Unload")],
            _added!.Steps.Select(s => (s.Sequence, s.NodeCode, s.Action)));
    }

    [Fact]
    public async Task Writes_first_history_row()
    {
        await Create(Request);

        var row = Assert.Single(_added!.StatusHistory);
        Assert.Null(row.PreviousStatus);
        Assert.Equal(MissionStatus.Created, row.Status);
        Assert.Equal(StatusSource.Api, row.Source);
        Assert.Equal(TestData.Now, row.ChangedAt);
    }

    [Fact]
    public async Task Saves_changes()
    {
        await Create(Request);

        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stores_existing_robot()
    {
        _store.RobotExistsAsync(7, Arg.Any<CancellationToken>()).Returns(true);

        await Create(Request with { RobotId = 7 });

        Assert.Equal(7, _added!.RobotId);
    }

    [Fact]
    public async Task Unknown_robot_is_a_validation_error()
    {
        _store.RobotExistsAsync(7, Arg.Any<CancellationToken>()).Returns(false);

        var invalid = Assert.IsType<CreateMissionResult.Invalid>(await Create(Request with { RobotId = 7 }));

        Assert.Contains("robotId", invalid.Errors.Keys);
    }

    [Fact]
    public async Task Unknown_robot_stores_nothing()
    {
        _store.RobotExistsAsync(7, Arg.Any<CancellationToken>()).Returns(false);

        await Create(Request with { RobotId = 7 });

        _store.DidNotReceive().Add(Arg.Any<Mission>());
        await _store.DidNotReceive().NextMissionCodeNumberAsync(Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_request_stores_nothing()
    {
        var result = await Create(Request with { Steps = [] });

        Assert.IsType<CreateMissionResult.Invalid>(result);
        _store.DidNotReceive().Add(Arg.Any<Mission>());
        await _store.DidNotReceive().NextMissionCodeNumberAsync(Arg.Any<CancellationToken>());
    }
}
