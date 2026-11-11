using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Auth;

public sealed class CurrentApiClient
{
    public ApiClient? Client { get; set; }

    public int Id => Client?.Id ?? throw new InvalidOperationException("No authenticated API client.");
}
