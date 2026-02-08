using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FesStarter.Core.Idempotency;

/// <summary>
/// In-memory implementation of idempotency service.
/// For production, use Redis or distributed cache.
/// </summary>
/// <remarks>
/// Uses SemaphoreSlim per key to prevent race conditions where multiple simultaneous
/// requests with the same idempotency key could both execute the handler.
/// Implements double-check locking pattern: check cache → acquire lock → check again.
/// </remarks>
public class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryIdempotencyService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

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

        // First check: fast path for cache hits without locking
        if (_cache.TryGetValue(cacheKey, out T? cachedResult))
        {
            _logger.LogInformation(
                "Idempotency cache hit for key={Key}, returning cached result",
                idempotencyKey);
            return cachedResult;
        }

        // Get or create semaphore for this key
        var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(ct);
        try
        {
            // Second check: after acquiring lock, check cache again (double-check locking)
            if (_cache.TryGetValue(cacheKey, out T? cachedAgain))
            {
                _logger.LogInformation(
                    "Idempotency cache hit for key={Key} (after lock acquisition), returning cached result",
                    idempotencyKey);
                return cachedAgain;
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
        finally
        {
            semaphore.Release();
        }
    }
}
