using System.Diagnostics;

namespace FesStarter.Api.Infrastructure;

/// <summary>
/// Middleware that extracts correlation ID from request headers and stores it for the request scope.
/// Adds correlation ID to response headers for client-side tracing.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string CausationIdHeader = "X-Causation-ID";

    public async Task InvokeAsync(HttpContext context, CorrelationContext correlationContext)
    {
        // Extract from request headers or generate new
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        var causationId = context.Request.Headers.TryGetValue(CausationIdHeader, out var causationValue)
            ? causationValue.ToString()
            : null;

        // Initialize correlation context
        correlationContext.Initialize(correlationId, causationId);

        // Set Activity for distributed tracing
        var activity = Activity.Current ?? new Activity("HttpRequest").Start();
        activity?.SetTag("correlation-id", correlationId);
        if (causationId != null)
        {
            activity?.SetTag("causation-id", causationId);
        }

        // Add to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        logger.LogInformation(
            "Request started - CorrelationId={CorrelationId}, Path={Path}",
            correlationId,
            context.Request.Path);

        try
        {
            await next(context);
        }
        finally
        {
            activity?.Stop();
            logger.LogInformation(
                "Request completed - CorrelationId={CorrelationId}, Status={Status}",
                correlationId,
                context.Response.StatusCode);
        }
    }
}

/// <summary>
/// Extension method to register correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
