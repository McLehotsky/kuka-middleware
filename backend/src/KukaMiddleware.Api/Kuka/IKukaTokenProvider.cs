using Microsoft.Extensions.Options;

namespace KukaMiddleware.Api.Kuka;

// Q1 option A: a static token from configuration. A login-based provider (vendor §4.7, not supplied)
// can replace it behind this interface.
public interface IKukaTokenProvider
{
    /// <summary>Null when no token is configured.</summary>
    ValueTask<string?> GetTokenAsync(CancellationToken ct);
}

public sealed class StaticKukaTokenProvider(IOptions<KukaFmOptions> options) : IKukaTokenProvider
{
    public ValueTask<string?> GetTokenAsync(CancellationToken ct) =>
        ValueTask.FromResult(string.IsNullOrWhiteSpace(options.Value.AccessToken) ? null : options.Value.AccessToken);
}
