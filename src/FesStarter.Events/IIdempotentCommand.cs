namespace FesStarter.Events;

/// <summary>
/// Marks a command as supporting idempotency through keys.
/// Prevents duplicate command processing when retries occur.
/// </summary>
public interface IIdempotentCommand
{
    /// <summary>
    /// Unique key for this command invocation.
    /// Should be stable across retries (e.g., from idempotency-key header).
    /// </summary>
    string IdempotencyKey { get; }
}
