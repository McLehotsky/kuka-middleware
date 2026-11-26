namespace KukaMiddleware.Api.Kuka;

// Configuration section "KukaFm" (system-definition §11, §14 Phase 5 scope). The repo holds defaults only;
// the access token comes from the environment (KukaFm__AccessToken).
public sealed record KukaFmOptions
{
    public const string Section = "KukaFm";
    private static readonly string[] CancelModes = ["FORCE", "NORMAL", "REDIRECT_END", "REDIRECT_START"];

    public string BaseUrl { get; set; } = "";

    /// <summary>Sent as the raw <c>Authorization</c> header value; empty = no header.</summary>
    public string? AccessToken { get; set; }

    public string OrgId { get; set; } = "";
    public string CancelMode { get; set; } = "FORCE";
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary><c>waitingMillis</c> of a <c>Wait</c> step.</summary>
    public int WaitMillis { get; set; }

    public bool IsValid() =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
        !string.IsNullOrWhiteSpace(OrgId) &&
        CancelModes.Contains(CancelMode) &&
        TimeoutSeconds is > 0 and <= 300 &&
        WaitMillis >= 0;
}
