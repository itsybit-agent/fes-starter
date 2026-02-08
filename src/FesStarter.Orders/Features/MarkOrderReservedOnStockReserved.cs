using FileEventStore.Session;
using FesStarter.Events;
using FesStarter.Events.Inventory;
using FesStarter.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FesStarter.Orders;

/// <summary>
/// Translates StockReserved event (from Inventory context) to order state change.
/// When inventory confirms stock is reserved, mark the order as placed/reserved.
/// </summary>
public class MarkOrderReservedOnStockReservedHandler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher,
    ILogger<MarkOrderReservedOnStockReservedHandler> logger) :
    INotificationHandler<DomainEventNotification<StockReserved>>
{
    public async Task Handle(DomainEventNotification<StockReserved> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        logger.LogInformation("Marking order as reserved for Order {OrderId}", evt.OrderId);

        await using var session = sessionFactory.OpenSession();

        var order = await session.AggregateStreamAsync<OrderAggregate>(evt.OrderId);
        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found, cannot mark as reserved", evt.OrderId);
            return;
        }

        order.MarkReserved();
        var events = order.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events, ct);
    }
}
