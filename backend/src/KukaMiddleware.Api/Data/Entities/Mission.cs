namespace KukaMiddleware.Api.Data.Entities;

public class Mission
{
    public Guid Id { get; set; }
    public required string MissionCode { get; set; }
    public string? ExternalRef { get; set; }
    public required string MissionType { get; set; }
    public int Priority { get; set; }
    public MissionStatus Status { get; set; }
    public int? RobotId { get; set; }
    public Robot? Robot { get; set; }
    public int CreatedByClientId { get; set; }
    public ApiClient CreatedByClient { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<MissionStep> Steps { get; } = [];
    public List<MissionStatusHistory> StatusHistory { get; } = [];
}
