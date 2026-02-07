using System.Collections.Concurrent;
using FileEventStore;
using FesStarter.Api.Domain.Orders;

namespace FesStarter.Api.Features.Orders;

public class OrderReadModel
{
    private readonly ConcurrentDictionary<string, OrderDto> _orders = new();

    public void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case OrderPlaced e:
                _orders[e.OrderId] = new OrderDto(
                    e.OrderId, 
                    e.ProductId, 
                    e.Quantity, 
                    "Pending", 
                    e.PlacedAt, 
                    null);
                break;
            case OrderStockReserved e:
                if (_orders.TryGetValue(e.OrderId, out var pendingOrder))
                {
                    _orders[e.OrderId] = pendingOrder with { Status = "Placed" };
                }
                break;
            case OrderShipped e:
                if (_orders.TryGetValue(e.OrderId, out var placedOrder))
                {
                    _orders[e.OrderId] = placedOrder with { Status = "Shipped", ShippedAt = e.ShippedAt };
                }
                break;
        }
    }

    public List<OrderDto> GetAll() => _orders.Values.OrderByDescending(o => o.PlacedAt).ToList();
    
    public OrderDto? Get(string orderId) => _orders.GetValueOrDefault(orderId);
}

public record OrderDto(
    string OrderId, 
    string ProductId, 
    int Quantity, 
    string Status, 
    DateTime PlacedAt, 
    DateTime? ShippedAt);
