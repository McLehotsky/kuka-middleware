using System.Security.Cryptography;
using System.Text;

namespace KukaMiddleware.Api.Auth;

public sealed class ApiKeySettings
{
    public string WebUiHash { get; }
    public string ExternalSystemHash { get; }

    public ApiKeySettings(string webUiKey, string externalSystemKey)
    {
        Validate(webUiKey, "ApiKeys:WebUi");
        Validate(externalSystemKey, "ApiKeys:ExternalSystem");
        if (webUiKey == externalSystemKey)
            throw new InvalidOperationException("API clients must have different keys.");
        WebUiHash = Hash(webUiKey);
        ExternalSystemHash = Hash(externalSystemKey);
    }

    public static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    private static void Validate(string? key, string setting)
    {
        if (key is not { Length: 64 } || !key.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'))
            throw new InvalidOperationException($"{setting} must be a 32-byte lowercase hex key. Run docker/start.bat to configure local keys.");
    }
}
