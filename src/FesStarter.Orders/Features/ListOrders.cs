using System.Collections.Concurrent;
using FileEventStore;
using FesStarter.Events;
using FesStarter.Events.Orders;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FesStarter.Orders;

public record OrderDto(string OrderId, string ProductId, int Quantity, string Status, DateTime PlacedAt, DateTime? ShippedAt);

public class OrderReadModel
{
    private readonly ConcurrentDictionary<string, OrderDto> _orders = new();

    public void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case OrderPlaced e:
                _orders[e.OrderId] = new OrderDto(e.OrderId, e.ProductId, e.Quantity, "Pending", e.PlacedAt, null);
                break;
            case OrderStockReserved e:
                // Idempotent: create if doesn't exist (handles async projection ordering)
                if (!_orders.TryGetValue(e.OrderId, out var order))
                {
                    // Order not yet in read model - create placeholder
                    _orders[e.OrderId] = new OrderDto(e.OrderId, "", 0, "Placed", DateTime.UtcNow, null);
                }
                else
                {
                    _orders[e.OrderId] = order with { Status = "Placed" };
                }
                break;
            case OrderShipped e:
                // Idempotent: create if doesn't exist
                if (!_orders.TryGetValue(e.OrderId, out var placed))
                {
                    _orders[e.OrderId] = new OrderDto(e.OrderId, "", 0, "Shipped", DateTime.UtcNow, e.ShippedAt);
                }
                else
                {
                    _orders[e.OrderId] = placed with { Status = "Shipped", ShippedAt = e.ShippedAt };
                }
                break;
        }
    }

    public List<OrderDto> GetAll() => _orders.Values.OrderByDescending(o => o.PlacedAt).ToList();
    public OrderDto? Get(string orderId) => _orders.GetValueOrDefault(orderId);
}

public class OrderReadModelProjections(OrderReadModel readModel, ILogger<OrderReadModelProjections> logger) :
    INotificationHandler<DomainEventNotification<OrderPlaced>>,
    INotificationHandler<DomainEventNotification<OrderStockReserved>>,
    INotificationHandler<DomainEventNotification<OrderShipped>>
{
    public Task Handle(DomainEventNotification<OrderPlaced> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting OrderPlaced to read model: {OrderId}", notification.DomainEvent.OrderId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<OrderStockReserved> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting OrderStockReserved to read model: {OrderId}", notification.DomainEvent.OrderId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<OrderShipped> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting OrderShipped to read model: {OrderId}", notification.DomainEvent.OrderId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

public class ListOrdersHandler(OrderReadModel readModel)
{
    public Task<List<OrderDto>> HandleAsync() => Task.FromResult(readModel.GetAll());
}

public static class ListOrdersEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapGet("/api/orders", async (ListOrdersHandler handler) => Results.Ok(await handler.HandleAsync()))
        .WithName("ListOrders")
        .WithTags("Orders");
}
