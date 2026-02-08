using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FesStarter.Core.Idempotency;

/// <summary>
/// In-memory implementation of idempotency service.
/// For production, use Redis or distributed cache.
/// </summary>
/// <remarks>
/// ⚠️ RACE CONDITION: The current check-then-execute pattern is not atomic.
/// Two simultaneous requests with the same key could both execute before caching.
/// 
/// For production, consider using a SemaphoreSlim per key:
/// <code>
/// private readonly ConcurrentDictionary&lt;string, SemaphoreSlim&gt; _locks = new();
/// 
/// var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
/// await semaphore.WaitAsync(ct);
/// try
/// {
///     // Check cache again after acquiring lock
///     if (_cache.TryGetValue(cacheKey, out T? cached)) return cached;
///     var result = await executor();
///     _cache.Set(cacheKey, result, expiration);
///     return result;
/// }
/// finally
/// {
///     semaphore.Release();
/// }
/// </code>
/// 
/// Or use Redis with SETNX for distributed locking.
/// </remarks>
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
