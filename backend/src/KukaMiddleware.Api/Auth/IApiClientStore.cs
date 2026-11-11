using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Auth;

public interface IApiClientStore
{
    Task<ApiClient?> FindByKeyHashAsync(string hash, CancellationToken ct);
    Task ConfigureAsync(ApiKeySettings keys, CancellationToken ct);
}
