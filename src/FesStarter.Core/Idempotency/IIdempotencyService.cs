namespace FesStarter.Core.Idempotency;

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

    /// <summary>
    /// Execute a command without returning a result, caching execution by idempotency key.
    /// If the key is already cached, skips execution and returns immediately.
    /// </summary>
    Task GetOrExecuteAsync(
        string idempotencyKey,
        Func<Task> executor,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}
