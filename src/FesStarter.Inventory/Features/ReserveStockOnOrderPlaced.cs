using FileEventStore.Session;
using FesStarter.Events;
using FesStarter.Events.Orders;
using FesStarter.Inventory;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FesStarter.Inventory;

/// <summary>
/// Translates OrderPlaced event (from Orders context) to stock reservation.
/// This is a cross-context translation - when an order is placed, reserve inventory.
/// </summary>
public class ReserveStockOnOrderPlacedHandler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher,
    ILogger<ReserveStockOnOrderPlacedHandler> logger) :
    INotificationHandler<DomainEventNotification<OrderPlaced>>
{
    public async Task Handle(DomainEventNotification<OrderPlaced> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        logger.LogInformation("Reserving stock for Order {OrderId}, Product {ProductId}, Quantity {Quantity}",
            evt.OrderId, evt.ProductId, evt.Quantity);

        await using var session = sessionFactory.OpenSession();

        var stock = await session.AggregateStreamAsync<ProductStockAggregate>(evt.ProductId);
        if (stock == null)
        {
            logger.LogWarning("No stock found for product {ProductId}, cannot reserve for order {OrderId}",
                evt.ProductId, evt.OrderId);
            return;
        }

        stock.Reserve(evt.Quantity, evt.OrderId);
        var events = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events, ct);
    }
}
