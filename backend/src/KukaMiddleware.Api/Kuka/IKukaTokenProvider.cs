using Microsoft.Extensions.Options;

namespace KukaMiddleware.Api.Kuka;

// KUKA.AMR Fleet 2.15 §4.7 describes generating an account token in the KUKA FM UI.
// The middleware reads this token from configuration.
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
