using FileEventStore;

namespace FesStarter.Events.Orders;

public record OrderPlaced(string OrderId, string ProductId, int Quantity, DateTime PlacedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}

public record OrderStockReserved(string OrderId, DateTime ReservedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}

public record OrderShipped(string OrderId, DateTime ShippedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}
