using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Kuka;
using KukaMiddleware.Api.Missions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Missions;

public class CreateMissionTests
{
    private static readonly CreateMissionRequest Request =
        new("RACK_MOVE", null, null, "ERP-1", [new("N-01", "Load"), new("N-02", "Wait"), new("N-03", "Unload")]);

    private readonly IMissionStore _store = Substitute.For<IMissionStore>();
    private readonly IKukaClient _kuka = TestData.Kuka();
    private readonly MissionService _service;
    private readonly List<MissionStatus> _savedStatuses = [];
    private Mission? _added;

    public CreateMissionTests()
    {
        _store.NextMissionCodeNumberAsync(Arg.Any<CancellationToken>()).Returns(42L);
        _store.Add(Arg.Do<Mission>(m => _added = m));
        _store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask)
            .AndDoes(_ => _savedStatuses.Add(_added!.Status));
        _service = new MissionService(_store, _kuka, TestData.Time(), TestData.Client());
    }

    private Task<CreateMissionResult> Create(CreateMissionRequest request) =>
        _service.CreateAsync(request, TestContext.Current.CancellationToken);

    private void KukaFails(string error = "KUKA FM is unreachable.") =>
        _kuka.SubmitMissionAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new KukaResult(error));

    // --- edge cases: invalid input never reaches KUKA FM ---

    [Fact]
    public async Task Invalid_request_stores_nothing_and_does_not_call_kuka()
    {
        var result = await Create(Request with { Steps = [] });

        Assert.IsType<CreateMissionResult.Invalid>(result);
        _store.DidNotReceive().Add(Arg.Any<Mission>());
        await _store.DidNotReceive().NextMissionCodeNumberAsync(Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _kuka.DidNotReceiveWithAnyArgs().SubmitMissionAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Null_step_is_invalid_and_stores_nothing()
    {
        var result = await Create(Request with { Steps = [new("N-01", "Load"), null!] });

        var invalid = Assert.IsType<CreateMissionResult.Invalid>(result);
        Assert.Contains("steps[1].nodeCode", invalid.Errors.Keys);
        _store.DidNotReceive().Add(Arg.Any<Mission>());
        await _kuka.DidNotReceiveWithAnyArgs().SubmitMissionAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Roller_move_is_rejected_until_bin_codes_are_decided()
    {
        var result = await Create(Request with { MissionType = "ROLLER_MOVE" });

        var invalid = Assert.IsType<CreateMissionResult.Invalid>(result);
        Assert.Contains("missionType", invalid.Errors.Keys);
        _store.DidNotReceive().Add(Arg.Any<Mission>());
        await _kuka.DidNotReceiveWithAnyArgs().SubmitMissionAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_robot_is_a_validation_error()
    {
        var invalid = Assert.IsType<CreateMissionResult.Invalid>(await Create(Request with { RobotId = 7 }));

        Assert.Contains("robotId", invalid.Errors.Keys);
    }

    [Fact]
    public async Task Unknown_robot_stores_nothing_and_does_not_call_kuka()
    {
        await Create(Request with { RobotId = 7 });

        _store.DidNotReceive().Add(Arg.Any<Mission>());
        await _store.DidNotReceive().NextMissionCodeNumberAsync(Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _kuka.DidNotReceiveWithAnyArgs().SubmitMissionAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    // --- KUKA FM rejects or is unreachable ---

    [Fact]
    public async Task Kuka_failure_returns_failed_with_the_mission_id()
    {
        KukaFails();

        var failed = Assert.IsType<CreateMissionResult.KukaFailed>(await Create(Request));

        Assert.Equal(_added!.Id, failed.MissionId);
        Assert.Equal("KUKA FM is unreachable.", failed.Error);
    }

    [Fact]
    public async Task Kuka_failure_marks_the_mission_failed_and_finished()
    {
        KukaFails();

        await Create(Request);

        Assert.Equal(MissionStatus.Failed, _added!.Status);
        Assert.Equal("KUKA FM is unreachable.", _added.ErrorMessage);
        Assert.Equal(TestData.Now, _added.FinishedAt);
        Assert.Null(_added.SubmittedAt);
    }

    [Fact]
    public async Task Kuka_failure_writes_failed_history_row()
    {
        KukaFails();

        await Create(Request);

        var row = _added!.StatusHistory[^1];
        Assert.Equal(MissionStatus.Created, row.PreviousStatus);
        Assert.Equal(MissionStatus.Failed, row.Status);
        Assert.Equal(StatusSource.Api, row.Source);
        Assert.Equal("KUKA FM is unreachable.", row.Message);
    }

    [Fact]
    public async Task Kuka_failure_is_saved()
    {
        KukaFails();

        await Create(Request);

        Assert.Equal([MissionStatus.Created, MissionStatus.Failed], _savedStatuses);
    }

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(1001, 1000)]
    [InlineData(5000, 1000)]
    public async Task Long_kuka_error_is_truncated_to_the_column_length(int length, int stored)
    {
        KukaFails(new string('x', length));

        await Create(Request);

        Assert.Equal(stored, _added!.ErrorMessage!.Length);
        Assert.Equal(stored, _added.StatusHistory[^1].Message!.Length);
    }

    [Fact]
    public async Task Concurrent_cancel_after_submit_still_returns_created()
    {
        var saves = 0;
        _store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            ++saves == 2 ? Task.FromException(new DbUpdateConcurrencyException("cancelled meanwhile")) : Task.CompletedTask);

        var created = Assert.IsType<CreateMissionResult.Created>(await Create(Request));

        Assert.Equal(_added!.Id, created.Mission.Id);
    }

    // --- success ---

    [Fact]
    public async Task Mission_is_stored_as_created_before_kuka_is_called()
    {
        MissionStatus? statusAtCall = null;
        _kuka.SubmitMissionAsync(Arg.Do<Mission>(_ => statusAtCall = _savedStatuses.LastOrDefault()), Arg.Any<CancellationToken>())
            .Returns(KukaResult.Ok);

        await Create(Request);

        Assert.Equal(MissionStatus.Created, statusAtCall);
    }

    [Fact]
    public async Task Submits_the_stored_mission_to_kuka()
    {
        await Create(Request);

        await _kuka.Received(1).SubmitMissionAsync(_added!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Kuka_success_marks_the_mission_submitted()
    {
        var result = await Create(Request);

        Assert.IsType<CreateMissionResult.Created>(result);
        Assert.Equal(MissionStatus.Submitted, _added!.Status);
        Assert.Equal(TestData.Now, _added.SubmittedAt);
        Assert.Null(_added.FinishedAt);
        Assert.Null(_added.ErrorMessage);
    }

    [Fact]
    public async Task Kuka_success_writes_created_and_submitted_history()
    {
        await Create(Request);

        Assert.Equal(
            [(null, MissionStatus.Created), (MissionStatus.Created, MissionStatus.Submitted)],
            _added!.StatusHistory.Select(h => (h.PreviousStatus, h.Status)));
        Assert.All(_added.StatusHistory, h => Assert.Equal(StatusSource.Api, h.Source));
        Assert.All(_added.StatusHistory, h => Assert.Equal(TestData.Now, h.ChangedAt));
    }

    [Fact]
    public async Task Kuka_success_is_saved()
    {
        await Create(Request);

        Assert.Equal([MissionStatus.Created, MissionStatus.Submitted], _savedStatuses);
    }

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
    public async Task Stores_created_at_from_time_provider()
    {
        await Create(Request);

        Assert.Equal(TestData.Now, _added!.CreatedAt);
    }

    [Fact]
    public async Task Attributes_mission_to_authenticated_client()
    {
        var client = TestData.Client();
        client.Client!.Id = 7;
        var service = new MissionService(_store, _kuka, TestData.Time(), client);

        await service.CreateAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal(7, _added!.CreatedByClientId);
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
    public async Task Chosen_robot_is_stored_and_passed_to_kuka()
    {
        var robot = new Robot { Id = 7, KukaRobotId = "14" };
        _store.FindRobotAsync(7, Arg.Any<CancellationToken>()).Returns(robot);

        await Create(Request with { RobotId = 7 });

        Assert.Equal(7, _added!.RobotId);
        await _kuka.Received(1).SubmitMissionAsync(Arg.Is<Mission>(m => m.Robot == robot), Arg.Any<CancellationToken>());
    }
}
