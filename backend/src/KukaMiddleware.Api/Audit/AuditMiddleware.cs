using System.Text.Json;
using KukaMiddleware.Api.Auth;
using KukaMiddleware.Api.Data.Entities;

namespace KukaMiddleware.Api.Audit;

public sealed class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, CurrentApiClient current, IAuditWriter writer, TimeProvider time)
    {
        var segments = context.Request.Path.Value?.Trim('/').Split('/') ?? [];
        var operation = HttpMethods.IsPost(context.Request.Method) && segments.Length >= 2 &&
            segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) &&
            segments[1].Equals("missions", StringComparison.OrdinalIgnoreCase);
        var create = operation && segments.Length == 2;
        var cancel = operation && segments.Length == 4 && segments[3].Equals("cancel", StringComparison.OrdinalIgnoreCase);
        if (!create && !cancel)
        {
            await next(context);
            return;
        }

        // Persist the audit before sending the small create/cancel response to the caller.
        var originalBody = context.Response.Body;
        await using var response = new MemoryStream();
        context.Response.Body = response;
        try
        {
            await next(context);
            if (current.Client is { } client)
            {
                var status = context.Response.StatusCode;
                Guid? missionId = null;
                if (cancel && status is 200 or 409 && Guid.TryParse(segments[2], out var cancelledId)) missionId = cancelledId;
                if (create && status == 201 && Guid.TryParse(context.Response.Headers.Location.ToString().Split('/').LastOrDefault(), out var createdId))
                    missionId = createdId;
                using var details = cancel ? JsonSerializer.SerializeToDocument(new { requestedMissionId = segments[2] }) : null;
                var entry = new AuditLog
                {
                    ApiClientId = client.Id,
                    MissionId = missionId,
                    Action = create ? AuditAction.DispatchMission : AuditAction.CancelMission,
                    Success = status is >= 200 and < 300,
                    HttpStatus = status,
                    Timestamp = time.GetUtcNow(),
                    CorrelationId = context.TraceIdentifier,
                    Details = details,
                };
                // Client disconnects must not discard the audit of an operation already executed.
                await writer.WriteAsync(entry, CancellationToken.None);
                logger.LogInformation("API operation {Action} by client {ClientId} returned {HttpStatus} {CorrelationId}",
                    entry.Action, client.Id, status, entry.CorrelationId);
            }
            response.Position = 0;
            await response.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
