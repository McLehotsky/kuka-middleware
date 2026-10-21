using System.Text.Json;

namespace KukaMiddleware.Api.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int ApiClientId { get; set; }
    public ApiClient ApiClient { get; set; } = null!;
    public Guid? MissionId { get; set; }
    public Mission? Mission { get; set; }
    public AuditAction Action { get; set; }
    public bool Success { get; set; }
    public int? HttpStatus { get; set; }
    public JsonDocument? Details { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
