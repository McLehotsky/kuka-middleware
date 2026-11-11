using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Audit;

// A separate context keeps failed mission writes out of the audit transaction.
public sealed class AuditWriter(IServiceScopeFactory scopes) : IAuditWriter
{
    public async Task WriteAsync(AuditLog entry, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<AuditLog>().Add(entry);
        await db.SaveChangesAsync(ct);
    }
}
