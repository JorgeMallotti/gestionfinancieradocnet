using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Middleware;

/// <summary>
/// Global exception handler → RFC 7807 ProblemDetails. Unexpected exceptions
/// never leak stack traces or internal messages to clients: production gets a
/// generic <c>detail</c>; development gets the full exception for debugging.
/// The full error is always written to Serilog with request context.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Client cancelled the request — not a server error; log at debug and stop.
        if (exception is OperationCanceledException)
        {
            logger.LogDebug("Request cancelled by client: {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            return true;
        }

        (int Status, string Title, string Detail) = exception switch
        {
            // Malformed JSON body, body over the size limit, invalid Content-Type...
            BadHttpRequestException badRequest =>
                (StatusCodes.Status400BadRequest, "Bad Request", badRequest.Message),

            // Anything else is a real server failure — keep the message internal.
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An unexpected error occurred while processing the request."),
        };

        if (Status >= 500)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected: {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        // If the response already started we can no longer write a body.
        if (httpContext.Response.HasStarted)
            return false;

        var problem = new ProblemDetails
        {
            Status = Status,
            Title = Title,
            Detail = Detail,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        var environment = httpContext.RequestServices?.GetService<IWebHostEnvironment>();
        if (environment?.IsDevelopment() == true)
            problem.Extensions["exception"] = exception.ToString();

        httpContext.Response.StatusCode = Status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
