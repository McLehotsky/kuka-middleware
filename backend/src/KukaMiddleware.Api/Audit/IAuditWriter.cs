using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Audit;

public interface IAuditWriter
{
    Task WriteAsync(AuditLog entry, CancellationToken ct);
}
