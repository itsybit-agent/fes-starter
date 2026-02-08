using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FesStarter.Core.Idempotency;

/// <summary>
/// Extension methods for idempotency handling.
/// </summary>
public static class IdempotencyExtensions
{
    /// <summary>
    /// Extract idempotency key from HTTP request headers.
    /// </summary>
    public static string? GetIdempotencyKey(this HttpRequest request)
    {
        const string header = "Idempotency-Key";
        return request.Headers.TryGetValue(header, out var value)
            ? value.ToString()
            : null;
    }

    /// <summary>
    /// Register the in-memory idempotency service.
    /// </summary>
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IIdempotencyService, InMemoryIdempotencyService>();
        return services;
    }
}
