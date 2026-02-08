using FileEventStore;
using MediatR;

namespace FesStarter.Events;

/// <summary>
/// Wraps domain events for MediatR notification publishing.
/// Allows event handlers to subscribe to specific event types via INotificationHandler.
/// </summary>
public record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification where TEvent : IStoreableEvent;
