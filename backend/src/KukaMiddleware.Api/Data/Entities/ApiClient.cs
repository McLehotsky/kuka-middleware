namespace KukaMiddleware.Api.Data.Entities;

public class ApiClient
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string KeyHash { get; set; }
    public ApiClientRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
