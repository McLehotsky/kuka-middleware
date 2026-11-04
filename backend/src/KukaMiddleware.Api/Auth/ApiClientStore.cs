using KukaMiddleware.Api.Data;
using KukaMiddleware.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KukaMiddleware.Api.Auth;

public sealed class ApiClientStore(AppDbContext db, TimeProvider time) : IApiClientStore
{
    public Task<ApiClient?> FindByKeyHashAsync(string hash, CancellationToken ct) =>
        db.Set<ApiClient>().AsNoTracking().SingleOrDefaultAsync(client => client.KeyHash == hash, ct);

    public async Task ConfigureAsync(ApiKeySettings keys, CancellationToken ct)
    {
        await ConfigureClientAsync("web-ui", keys.WebUiHash, ApiClientRole.Operator, ct);
        await ConfigureClientAsync("external-system", keys.ExternalSystemHash, ApiClientRole.ExternalSystem, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task ConfigureClientAsync(string name, string hash, ApiClientRole role, CancellationToken ct)
    {
        var client = await db.Set<ApiClient>().SingleOrDefaultAsync(c => c.Name == name, ct);
        if (client is null)
        {
            client = new ApiClient { Name = name, KeyHash = hash, Role = role, IsActive = true, CreatedAt = time.GetUtcNow() };
            db.Add(client);
        }
        client.KeyHash = hash;
    }
}
