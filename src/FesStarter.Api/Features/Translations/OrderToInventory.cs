using FileEventStore.Session;
using FesStarter.Events.Orders;
using FesStarter.Inventory;
using FesStarter.Orders;
using FesStarter.Api.Infrastructure;
using MediatR;

namespace FesStarter.Api.Features.Translations;

/// <summary>
/// Translates Order events into Inventory operations.
/// This is the "stream translation" pattern - events from one context trigger operations in another.
/// </summary>
public class OrderToInventoryHandler(
    IEventSessionFactory sessionFactory,
    StockReadModel stockReadModel,
    OrderReadModel orderReadModel,
    ILogger<OrderToInventoryHandler> logger) :
    INotificationHandler<DomainEventNotification<OrderPlaced>>,
    INotificationHandler<DomainEventNotification<OrderShipped>>
{
    public async Task Handle(DomainEventNotification<OrderPlaced> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        logger.LogInformation("Translating OrderPlaced -> StockReserved for Order {OrderId}", evt.OrderId);

        await using var session = sessionFactory.OpenSession();

        var stock = await session.AggregateStreamAsync<ProductStockAggregate>($"stock-{evt.ProductId}");
        if (stock == null)
        {
            logger.LogWarning("No stock found for product {ProductId}", evt.ProductId);
            return;
        }

        stock.Reserve(evt.Quantity, evt.OrderId);
        var stockEvents = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        foreach (var e in stockEvents) stockReadModel.Apply(e);

        // Mark order as reserved
        await using var orderSession = sessionFactory.OpenSession();
        var order = await orderSession.AggregateStreamAsync<OrderAggregate>($"order-{evt.OrderId}");
        if (order != null)
        {
            order.MarkReserved();
            var orderEvents = order.UncommittedEvents.ToList();
            await orderSession.SaveChangesAsync();
            foreach (var e in orderEvents) orderReadModel.Apply(e);
        }
    }

    public async Task Handle(DomainEventNotification<OrderShipped> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        logger.LogInformation("Translating OrderShipped -> StockDeducted for Order {OrderId}", evt.OrderId);

        var order = orderReadModel.Get(evt.OrderId);
        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found", evt.OrderId);
            return;
        }

        await using var session = sessionFactory.OpenSession();
        var stock = await session.AggregateStreamAsync<ProductStockAggregate>($"stock-{order.ProductId}");
        if (stock == null)
        {
            logger.LogWarning("No stock found for product {ProductId}", order.ProductId);
            return;
        }

        stock.Deduct(order.Quantity, evt.OrderId);
        var events = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        foreach (var e in events) stockReadModel.Apply(e);
    }
}
