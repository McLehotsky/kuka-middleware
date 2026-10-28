using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Robots;

public record RobotSummary(
    int Id,
    string KukaRobotId,
    string? Name,
    RobotStatus Status,
    int? BatteryLevel,
    string? CurrentNode,
    RobotSummary.MissionRef? CurrentMission,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset UpdatedAt)
{
    public record MissionRef(Guid Id, string MissionCode);
}
