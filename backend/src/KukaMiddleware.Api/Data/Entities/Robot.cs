namespace KukaMiddleware.Api.Data.Entities;

public class Robot
{
    public int Id { get; set; }
    public required string KukaRobotId { get; set; }
    public string? Name { get; set; }
    public RobotStatus Status { get; set; }
    public int? BatteryLevel { get; set; }
    public string? CurrentNode { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
