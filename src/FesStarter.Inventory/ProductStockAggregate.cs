using FileEventStore;
using FileEventStore.Aggregates;
using FesStarter.Events.Inventory;

namespace FesStarter.Inventory;

public class ProductStockAggregate : Aggregate
{
    public string ProductName { get; private set; } = "";
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }

    public int AvailableQuantity => QuantityOnHand - QuantityReserved;

    public void Initialize(string productId, string productName, int initialQuantity)
    {
        if (!string.IsNullOrEmpty(Id))
            throw new InvalidOperationException("Stock already initialized");

        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative", nameof(initialQuantity));

        Emit(new StockInitialized(productId, productName, initialQuantity, DateTime.UtcNow));
    }

    public void Reserve(int quantity, string orderId)
    {
        if (string.IsNullOrEmpty(Id))
            throw new InvalidOperationException("Stock not initialized");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (quantity > AvailableQuantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {AvailableQuantity}, Requested: {quantity}");

        Emit(new StockReserved(Id, quantity, orderId, DateTime.UtcNow));
    }

    public void Deduct(int quantity, string orderId)
    {
        if (string.IsNullOrEmpty(Id))
            throw new InvalidOperationException("Stock not initialized");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (quantity > QuantityReserved)
            throw new InvalidOperationException($"Cannot deduct more than reserved. Reserved: {QuantityReserved}, Requested: {quantity}");

        Emit(new StockDeducted(Id, quantity, orderId, DateTime.UtcNow));
    }

    public void Restock(int quantity)
    {
        if (string.IsNullOrEmpty(Id))
            throw new InvalidOperationException("Stock not initialized");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Emit(new StockRestocked(Id, quantity, DateTime.UtcNow));
    }

    protected override void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case StockInitialized e:
                Id = e.ProductId;
                ProductName = e.ProductName;
                QuantityOnHand = e.InitialQuantity;
                break;
            case StockReserved e:
                QuantityReserved += e.Quantity;
                break;
            case StockDeducted e:
                QuantityOnHand -= e.Quantity;
                QuantityReserved -= e.Quantity;
                break;
            case StockRestocked e:
                QuantityOnHand += e.Quantity;
                break;
        }
    }
}
