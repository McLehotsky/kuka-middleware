namespace KukaMiddleware.Api.Data.Entities;

public class MissionStatusHistory
{
    public long Id { get; set; }
    public Guid MissionId { get; set; }
    public MissionStatus? PreviousStatus { get; set; }
    public MissionStatus Status { get; set; }
    public StatusSource Source { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
