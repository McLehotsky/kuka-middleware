using KukaMiddleware.Api.Data.Entities;
using KukaMiddleware.Api.Missions;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Missions;

public class MissionDetailTests
{
    private readonly IMissionStore _store = Substitute.For<IMissionStore>();
    private readonly MissionService _service;

    public MissionDetailTests() => _service = new MissionService(_store, TestData.Time());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Mission Stored()
    {
        var mission = TestData.Mission();
        _store.FindAsync(mission.Id, Arg.Any<CancellationToken>()).Returns(mission);
        return mission;
    }

    private Guid StoredWithHistory(params MissionStatusHistory[] rows)
    {
        var id = Guid.NewGuid();
        _store.MissionExistsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        _store.ListHistoryAsync(id, Arg.Any<CancellationToken>()).Returns([.. rows]);
        return id;
    }

    [Fact]
    public async Task Unknown_mission_detail_returns_null()
    {
        Assert.Null(await _service.GetAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task Detail_contains_creating_client_name()
    {
        var mission = Stored();

        var detail = await _service.GetAsync(mission.Id, Ct);

        Assert.Equal("web-ui", detail!.CreatedBy);
    }

    [Fact]
    public async Task Detail_steps_are_ordered_by_sequence()
    {
        var mission = Stored();
        mission.Steps.AddRange([
            new MissionStep { Sequence = 2, NodeCode = "N-02", Action = "Unload" },
            new MissionStep { Sequence = 1, NodeCode = "N-01", Action = "Load" },
        ]);

        var detail = await _service.GetAsync(mission.Id, Ct);

        Assert.Equal([1, 2], detail!.Steps.Select(s => s.Sequence));
    }

    [Fact]
    public async Task Unknown_mission_history_returns_null()
    {
        Assert.Null(await _service.GetHistoryAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task History_is_ordered_by_changed_at_ascending()
    {
        var id = StoredWithHistory(
            Row(MissionStatus.Cancelling, TestData.Now),
            Row(MissionStatus.Created, TestData.Now.AddMinutes(-10)),
            Row(MissionStatus.Submitted, TestData.Now.AddMinutes(-5)));

        var history = await _service.GetHistoryAsync(id, Ct);

        Assert.Equal(
            [MissionStatus.Created, MissionStatus.Submitted, MissionStatus.Cancelling],
            history!.Select(h => h.Status));
    }

    private static MissionStatusHistory Row(MissionStatus status, DateTimeOffset changedAt) =>
        new() { Status = status, Source = StatusSource.Api, ChangedAt = changedAt };
}
