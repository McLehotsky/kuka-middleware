using KukaMiddleware.Api.Data.Entities;
using NSubstitute;

namespace KukaMiddleware.Api.Tests.Missions;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    public static TimeProvider Time()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        return time;
    }

    public static Mission Mission(MissionStatus status = MissionStatus.Created, DateTimeOffset? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        MissionCode = "M-2026-0001",
        MissionType = "RACK_MOVE",
        Priority = 2,
        Status = status,
        CreatedAt = createdAt ?? Now.AddHours(-1),
        CreatedByClientId = 1,
        CreatedByClient = new ApiClient { Id = 1, Name = "web-ui", KeyHash = "x" },
    };
}
