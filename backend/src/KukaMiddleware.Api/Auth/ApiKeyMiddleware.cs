namespace KukaMiddleware.Api.Auth;

public sealed class ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
{
    public const string HeaderName = "X-API-Key";

    public async Task InvokeAsync(HttpContext context, IApiClientStore store, CurrentApiClient current)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api") || string.Equals(path.Value?.TrimEnd('/'), "/api/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        current.Client = null;
        var values = context.Request.Headers[HeaderName];
        if (values.Count == 1 && values[0] is { Length: > 0 and <= 256 } key)
        {
            var client = await store.FindByKeyHashAsync(ApiKeySettings.Hash(key), context.RequestAborted);
            if (client is { IsActive: true })
            {
                current.Client = client;
                await next(context);
                return;
            }
        }

        logger.LogWarning("API authentication rejected {Method} {Path} {CorrelationId}",
            context.Request.Method, context.Request.Path, context.TraceIdentifier);
        await Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "API key missing or invalid")
            .ExecuteAsync(context);
    }
}
