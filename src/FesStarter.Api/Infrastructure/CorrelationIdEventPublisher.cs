using FileEventStore;
using FesStarter.Events;
using Microsoft.Extensions.Logging;

namespace FesStarter.Api.Infrastructure;

/// <summary>
/// Decorator that enriches all published events with correlation and causation IDs.
/// Enables distributed tracing through event-driven flows.
/// </summary>
public class CorrelationIdEventPublisher(
    IEventPublisher inner,
    CorrelationContext correlationContext,
    ILogger<CorrelationIdEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct = default)
    {
        var enrichedEvents = EnrichWithCorrelationIds(events).ToList();

        foreach (var evt in enrichedEvents)
        {
            if (evt is ICorrelatedEvent correlatedEvt)
            {
                logger.LogDebug(
                    "Publishing {EventType} with CorrelationId={CorrelationId}, CausationId={CausationId}",
                    evt.GetType().Name,
                    correlatedEvt.CorrelationId,
                    correlatedEvt.CausationId ?? "none");
            }
        }

        await inner.PublishAsync(enrichedEvents, ct);
    }

    private IEnumerable<IStoreableEvent> EnrichWithCorrelationIds(IEnumerable<IStoreableEvent> events)
    {
        foreach (var evt in events)
        {
            if (evt is ICorrelatedEvent correlatedEvt)
            {
                // Clone the event with correlation IDs set
                var enrichedEvent = CloneWithCorrelationIds(correlatedEvt);
                yield return enrichedEvent;
            }
            else
            {
                yield return evt;
            }
        }
    }

    private IStoreableEvent CloneWithCorrelationIds(ICorrelatedEvent evt)
    {
        // For records, use 'with' to create a copy with new values
        return evt switch
        {
            FesStarter.Events.Orders.OrderPlaced e => e with
            {
                CorrelationId = correlationContext.CorrelationId,
                CausationId = correlationContext.CausationId
            },
            FesStarter.Events.Orders.OrderStockReserved e => e with
            {
                CorrelationId = correlationContext.CorrelationId,
                CausationId = correlationContext.CausationId
            },
            FesStarter.Events.Orders.OrderShipped e => e with
            {
                CorrelationId = correlationContext.CorrelationId,
                CausationId = correlationContext.CausationId
            },
            FesStarter.Events.Inventory.StockInitialized e => e with
            {
                CorrelationId = correlationContext.CorrelationId,
                CausationId = correlationContext.CausationId
            },
            FesStarter.Events.Inventory.StockReserved e => e with
            {
                CorrelationId = correlationContext.CorrelationId,
                CausationId = correlationContext.CausationId
            },
            FesStarter.Events.Inventory.StockDeducted e => e with
            {
                CorrelationId = correlationContext.CorrelationId,
                CausationId = correlationContext.CausationId
            },
            FesStarter.Events.Inventory.StockRestocked e => e with
            {
                CorrelationId = correlationContext.CorrelationId,
                CausationId = correlationContext.CausationId
            },
            _ => evt
        };
    }
}
