using FileEventStore.Session;
using FesStarter.Events;
using FesStarter.Events.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FesStarter.Inventory;

/// <summary>
/// Translates OrderShipped event (from Orders context) to stock deduction.
/// When an order ships, deduct the reserved inventory.
/// </summary>
public class DeductStockOnOrderShippedHandler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher,
    ILogger<DeductStockOnOrderShippedHandler> logger) :
    INotificationHandler<DomainEventNotification<OrderShipped>>
{
    public async Task Handle(DomainEventNotification<OrderShipped> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        logger.LogInformation("Deducting stock for shipped Order {OrderId}, Product {ProductId}, Quantity {Quantity}",
            evt.OrderId, evt.ProductId, evt.Quantity);

        await using var session = sessionFactory.OpenSession();

        var stock = await session.AggregateStreamAsync<ProductStockAggregate>(evt.ProductId);
        if (stock == null)
        {
            logger.LogWarning("No stock found for product {ProductId}", evt.ProductId);
            return;
        }

        stock.Deduct(evt.Quantity, evt.OrderId);
        var events = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events, ct);
    }
}
