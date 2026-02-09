using FileEventStore;
using MediatR;
using FesStarter.Api.Infrastructure;

namespace FesStarter.FesModule.Features;

/// <summary>
/// Reacts to events from other bounded contexts.
/// Implements cross-context automation/translation.
/// </summary>
public class FesAutomation(
    IEventSession session,
    CorrelationContext correlationContext) 
    : INotificationHandler<DomainEventNotification<FesStarter.Events.OtherModule.SomeEvent>>
{
    public async Task Handle(
        DomainEventNotification<FesStarter.Events.OtherModule.SomeEvent> notification, 
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Set causation chain
        if (evt is ICorrelatedEvent correlated && correlated.CorrelationId.HasValue)
        {
            correlationContext.CorrelationId = correlated.CorrelationId.Value;
            correlationContext.CausationId = correlated.CausationId;
        }

        // Load aggregate and perform action
        var aggregate = await session.AggregateStreamOrCreateAsync<Domain.FesModuleAggregate>(
            evt.Id, ct);

        // TODO: Call aggregate method based on the event
        // aggregate.ReactToEvent(evt);

        await session.SaveChangesAsync(ct);
    }
}

// Placeholder for the event this automation reacts to
// Replace with actual event from another module
namespace FesStarter.Events.OtherModule
{
    public record SomeEvent(string Id) : IStoreableEvent, ICorrelatedEvent
    {
        public Guid? CorrelationId { get; init; }
        public Guid? CausationId { get; init; }
    }
}
