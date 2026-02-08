# scaffold-automation

Add an automation (cross-context event handler) to a module.

## Usage

```
/scaffold-automation {Module} {HandlerName} "{description}"
```

## Examples

```
/scaffold-automation Inventory ReserveStockOnOrderPlaced "Reserve stock when an order is placed"
/scaffold-automation Payments ChargeCardOnOrderConfirmed "Charge payment when order is confirmed"
/scaffold-automation Notifications SendEmailOnOrderShipped "Send email when order ships"
```

## What It Creates

- Event handler that reacts to events from another module
- No endpoint (internal automation)
- May emit its own events

## Steps

### 0. Detect project name

Find `*.slnx` or `*.sln` — filename is the ProjectName.

### 1. Find an automation to copy

Look in `src/{ProjectName}.{Module}/Features/` for files like `*On*.cs`.
Example: `ReserveStockOnOrderPlaced.cs`

### 2. Create the handler file

Copy existing automation, create `src/{ProjectName}.{Module}/Features/{HandlerName}.cs`:

```csharp
namespace {ProjectName}.{Module}.Features;

/// <summary>
/// {description}
/// Reacts to: {SourceEvent} from {SourceModule}
/// Emits: {ResultEvent} (if applicable)
/// </summary>
public class {HandlerName} : INotificationHandler<DomainEventNotification<{SourceEvent}>>
{
    private readonly IEventStore _eventStore;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<{HandlerName}> _logger;
    
    public {HandlerName}(
        IEventStore eventStore,
        IEventPublisher publisher,
        ILogger<{HandlerName}> logger)
    {
        _eventStore = eventStore;
        _publisher = publisher;
        _logger = logger;
    }
    
    public async Task Handle(
        DomainEventNotification<{SourceEvent}> notification, 
        CancellationToken ct)
    {
        var sourceEvent = notification.Event;
        
        _logger.LogInformation(
            "{Handler} reacting to {Event}", 
            nameof({HandlerName}), 
            sourceEvent.GetType().Name);
        
        // 1. Load aggregate (if needed)
        var streamId = $"{entity}-{sourceEvent.EntityId}";
        var aggregate = await _eventStore.AggregateStreamAsync<{Entity}Aggregate>(
            streamId, ct) ?? new {Entity}Aggregate();
        
        // 2. Perform action
        var resultEvent = aggregate.DoSomething(sourceEvent);
        
        // 3. Save event
        if (aggregate.Version == 0)
            await _eventStore.StartStreamAsync(streamId, resultEvent, ct: ct);
        else
            await _eventStore.AppendToStreamAsync(streamId, resultEvent, ct: ct);
        
        // 4. Publish for other handlers
        await _publisher.PublishAsync(resultEvent, ct);
    }
}
```

### 3. Naming convention

Pattern: `{Action}On{TriggerEvent}`

Examples:
| Handler | Reacts to | From Module |
|---------|-----------|-------------|
| `ReserveStockOnOrderPlaced` | `OrderPlaced` | Orders |
| `DeductStockOnOrderShipped` | `OrderShipped` | Orders |
| `SendEmailOnPaymentFailed` | `PaymentFailed` | Payments |
| `UpdateOrderOnStockReserved` | `StockReserved` | Inventory |

### 4. Add event reference

Make sure the module references the events project:

```xml
<ProjectReference Include="..\{ProjectName}.Events\{ProjectName}.Events.csproj" />
```

### 5. Handler is auto-registered

MediatR scans for `INotificationHandler<>` implementations.
No manual registration needed.

## Patterns

### Simple translation (emit event in response)
```csharp
// OrderPlaced → StockReserved
var reserved = aggregate.ReserveStock(sourceEvent.ProductId, sourceEvent.Quantity);
await _eventStore.AppendToStreamAsync(streamId, reserved, ct: ct);
await _publisher.PublishAsync(reserved, ct);
```

### Update state only (no new event)
```csharp
// StockReserved → Update OrderAggregate status
var marked = aggregate.MarkAsReserved();
await _eventStore.AppendToStreamAsync(streamId, marked, ct: ct);
```

### Side effect only (external call)
```csharp
// OrderShipped → Send email
await _emailService.SendAsync(sourceEvent.CustomerEmail, "Your order shipped!");
// Consider outbox pattern for reliability
```

## Reference

Copy from `ReserveStockOnOrderPlaced.cs` or similar `*On*.cs` files.
