using FileEventStore;

namespace FesStarter.Events.Orders;

public record OrderPlaced(string OrderId, string ProductId, int Quantity, DateTime PlacedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}

public record OrderStockReserved(string OrderId, DateTime ReservedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}

public record OrderShipped(string OrderId, DateTime ShippedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}
