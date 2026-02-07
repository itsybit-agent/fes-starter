using System.Collections.Concurrent;
using FileEventStore;
using FesStarter.Contracts.Orders;

namespace FesStarter.Api.Features.Orders;

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
                if (_orders.TryGetValue(e.OrderId, out var pending))
                    _orders[e.OrderId] = pending with { Status = "Placed" };
                break;
            case OrderShipped e:
                if (_orders.TryGetValue(e.OrderId, out var placed))
                    _orders[e.OrderId] = placed with { Status = "Shipped", ShippedAt = e.ShippedAt };
                break;
        }
    }

    public List<OrderDto> GetAll() => _orders.Values.OrderByDescending(o => o.PlacedAt).ToList();
    public OrderDto? Get(string orderId) => _orders.GetValueOrDefault(orderId);
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
