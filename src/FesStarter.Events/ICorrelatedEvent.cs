using FileEventStore;

namespace FesStarter.Events;

/// <summary>
/// Marks an event as part of a distributed trace with correlation and causation IDs.
/// Enables tracing requests through event-driven systems.
/// </summary>
public interface ICorrelatedEvent : IStoreableEvent
{
    /// <summary>
    /// Trace ID that links all events from a single user request through the entire system.
    /// </summary>
    string CorrelationId { get; init; }

    /// <summary>
    /// ID of the event that caused this event (for explicit causality chains).
    /// </summary>
    string? CausationId { get; init; }
}
