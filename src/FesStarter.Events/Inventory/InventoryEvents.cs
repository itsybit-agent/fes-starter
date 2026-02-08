using FileEventStore;

namespace FesStarter.Events.Inventory;

public record StockInitialized(string ProductId, string ProductName, int InitialQuantity, DateTime InitializedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}

public record StockReserved(string ProductId, int Quantity, string OrderId, DateTime ReservedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}

public record StockDeducted(string ProductId, int Quantity, string OrderId, DateTime DeductedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}

public record StockRestocked(string ProductId, int Quantity, DateTime RestockedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}
