namespace KukaMiddleware.Api.Data.Entities;

public class MissionStep
{
    public int Id { get; set; }
    public Guid MissionId { get; set; }
    public int Sequence { get; set; }
    public required string NodeCode { get; set; }
    public required string Action { get; set; }
}
