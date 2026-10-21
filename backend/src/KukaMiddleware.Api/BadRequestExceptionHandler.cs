using Microsoft.AspNetCore.Diagnostics;

namespace KukaMiddleware.Api;

// Malformed JSON / unbindable parameters are client errors: answer 400 ProblemDetails and log
// a warning without a stack trace instead of an unhandled-exception error.
public class BadRequestExceptionHandler(IProblemDetailsService problemDetails, ILogger<BadRequestExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not BadHttpRequestException bad)
            return false;

        logger.LogWarning("Bad request {Method} {Path}: {Reason}", context.Request.Method, context.Request.Path, bad.Message);
        context.Response.StatusCode = bad.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, Exception = exception });
    }
}
