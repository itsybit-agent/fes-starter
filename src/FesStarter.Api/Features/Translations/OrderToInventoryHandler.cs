using FileEventStore.Session;
using FesStarter.Api.Domain.Inventory;
using FesStarter.Api.Domain.Orders;
using FesStarter.Api.Features.Inventory;
using FesStarter.Api.Features.Orders;
using FesStarter.Api.Infrastructure;
using MediatR;

namespace FesStarter.Api.Features.Translations;

/// <summary>
/// Translates Order events into Inventory operations.
/// This is the "stream translation" pattern - events from one context
/// trigger commands/events in another context.
/// </summary>
public class OrderToInventoryHandler : 
    INotificationHandler<DomainEventNotification<OrderPlaced>>,
    INotificationHandler<DomainEventNotification<OrderShipped>>
{
    private readonly IEventSessionFactory _sessionFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly StockReadModel _stockReadModel;
    private readonly OrderReadModel _orderReadModel;
    private readonly ILogger<OrderToInventoryHandler> _logger;

    public OrderToInventoryHandler(
        IEventSessionFactory sessionFactory,
        IEventPublisher eventPublisher,
        StockReadModel stockReadModel,
        OrderReadModel orderReadModel,
        ILogger<OrderToInventoryHandler> logger)
    {
        _sessionFactory = sessionFactory;
        _eventPublisher = eventPublisher;
        _stockReadModel = stockReadModel;
        _orderReadModel = orderReadModel;
        _logger = logger;
    }

    /// <summary>
    /// When an order is placed, reserve stock for it.
    /// </summary>
    public async Task Handle(DomainEventNotification<OrderPlaced> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        _logger.LogInformation(
            "Translating OrderPlaced -> StockReserved for Order {OrderId}, Product {ProductId}, Qty {Quantity}",
            evt.OrderId, evt.ProductId, evt.Quantity);

        await using var session = _sessionFactory.OpenSession();
        
        var stock = await session.AggregateStreamAsync<ProductStockAggregate>($"stock-{evt.ProductId}");
        if (stock == null)
        {
            _logger.LogWarning("No stock found for product {ProductId}, cannot reserve", evt.ProductId);
            return;
        }

        stock.Reserve(evt.Quantity, evt.OrderId);
        
        var events = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        
        foreach (var e in events)
        {
            _stockReadModel.Apply(e);
        }
        
        // Now update the order to mark it as reserved
        await using var orderSession = _sessionFactory.OpenSession();
        var order = await orderSession.AggregateStreamAsync<OrderAggregate>($"order-{evt.OrderId}");
        if (order != null)
        {
            order.MarkReserved();
            var orderEvents = order.UncommittedEvents.ToList();
            await orderSession.SaveChangesAsync();
            
            foreach (var e in orderEvents)
            {
                _orderReadModel.Apply(e);
            }
        }
        
        _logger.LogInformation("Stock reserved successfully for Order {OrderId}", evt.OrderId);
    }

    /// <summary>
    /// When an order is shipped, deduct the reserved stock.
    /// </summary>
    public async Task Handle(DomainEventNotification<OrderShipped> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        _logger.LogInformation("Translating OrderShipped -> StockDeducted for Order {OrderId}", evt.OrderId);

        // Get order details from read model
        var order = _orderReadModel.Get(evt.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found in read model", evt.OrderId);
            return;
        }

        await using var session = _sessionFactory.OpenSession();
        
        var stock = await session.AggregateStreamAsync<ProductStockAggregate>($"stock-{order.ProductId}");
        if (stock == null)
        {
            _logger.LogWarning("No stock found for product {ProductId}", order.ProductId);
            return;
        }

        stock.Deduct(order.Quantity, evt.OrderId);
        
        var events = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        
        foreach (var e in events)
        {
            _stockReadModel.Apply(e);
        }
        
        _logger.LogInformation("Stock deducted successfully for Order {OrderId}", evt.OrderId);
    }
}
