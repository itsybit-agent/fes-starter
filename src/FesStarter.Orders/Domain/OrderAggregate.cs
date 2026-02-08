using FesStarter.Events.Orders;
using FileEventStore;
using FileEventStore.Aggregates;

namespace FesStarter.Orders;

public class OrderAggregate : Aggregate
{
    public string ProductId { get; private set; } = "";
    public int Quantity { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }

    public void Place(string orderId, string productId, int quantity)
    {
        if (!string.IsNullOrEmpty(Id))
            throw new InvalidOperationException("Order already exists");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Emit(new OrderPlaced(orderId, productId, quantity, DateTime.UtcNow));
    }

    public void Ship()
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException($"Cannot ship order in status {Status}");

        Emit(new OrderShipped(Id, DateTime.UtcNow));
    }

    public void MarkReserved()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot reserve order in status {Status}");

        Emit(new OrderStockReserved(Id, DateTime.UtcNow));
    }

    protected override void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case OrderPlaced e:
                Id = e.OrderId;
                ProductId = e.ProductId;
                Quantity = e.Quantity;
                CreatedAt = e.PlacedAt;
                Status = OrderStatus.Pending;
                break;
            case OrderStockReserved:
                Status = OrderStatus.Placed;
                break;
            case OrderShipped e:
                Status = OrderStatus.Shipped;
                ShippedAt = e.ShippedAt;
                break;
        }
    }
}

public enum OrderStatus
{
    Pending,
    Placed,
    Shipped
}
