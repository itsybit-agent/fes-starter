using FesStarter.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FesStarter.Api.Infrastructure;

/// <summary>
/// Caches command results by idempotency key to prevent duplicate processing.
/// Enables safe retries - same idempotency key returns cached result without re-executing.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Execute a command, caching the result by idempotency key.
    /// If the key is already cached, returns the cached result instead of executing.
    /// </summary>
    Task<T?> GetOrExecuteAsync<T>(
        string idempotencyKey,
        Func<Task<T>> executor,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}

/// <summary>
/// In-memory implementation of idempotency service.
/// For production, use Redis or distributed cache.
/// </summary>
public class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryIdempotencyService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);

    public InMemoryIdempotencyService(IMemoryCache cache, ILogger<InMemoryIdempotencyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrExecuteAsync<T>(
        string idempotencyKey,
        Func<Task<T>> executor,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            // No idempotency key, execute without caching
            return await executor();
        }

        var cacheKey = $"idempotency:{idempotencyKey}";

        // Check if already cached
        if (_cache.TryGetValue(cacheKey, out T? cachedResult))
        {
            _logger.LogInformation(
                "Idempotency cache hit for key={Key}, returning cached result",
                idempotencyKey);
            return cachedResult;
        }

        // Execute and cache
        _logger.LogDebug("Idempotency cache miss for key={Key}, executing handler", idempotencyKey);
        var result = await executor();

        var cacheExpiration = expiration ?? _defaultExpiration;
        _cache.Set(cacheKey, result, cacheExpiration);

        _logger.LogInformation(
            "Cached idempotent command result with key={Key}, expiration={Hours}h",
            idempotencyKey,
            cacheExpiration.TotalHours);

        return result;
    }
}

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
    /// Check if a command supports idempotency.
    /// </summary>
    public static bool SupportsIdempotency(this object command)
    {
        return command is IIdempotentCommand;
    }
}
