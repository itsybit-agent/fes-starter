using FileEventStore;

namespace FesStarter.Events.Inventory;

public record StockInitialized(string ProductId, string ProductName, int InitialQuantity, DateTime InitializedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}

public record StockReserved(string ProductId, int Quantity, string OrderId, DateTime ReservedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}

public record StockDeducted(string ProductId, int Quantity, string OrderId, DateTime DeductedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}

public record StockRestocked(string ProductId, int Quantity, DateTime RestockedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}
