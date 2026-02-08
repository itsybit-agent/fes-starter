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
using FileEventStore.Session;
using {ProjectName}.Events;
using {ProjectName}.Events.{SourceModule};
using MediatR;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.{Module};

/// <summary>
/// {description}
/// Reacts to: {SourceEvent} from {SourceModule}
/// Emits: {ResultEvent} (if applicable)
/// </summary>
public class {HandlerName}Handler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher,
    ILogger<{HandlerName}Handler> logger) :
    INotificationHandler<DomainEventNotification<{SourceEvent}>>
{
    public async Task Handle(
        DomainEventNotification<{SourceEvent}> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        
        logger.LogInformation(
            "{Handler} reacting to {Event} for {EntityId}",
            nameof({HandlerName}Handler),
            nameof({SourceEvent}),
            evt.{EntityId});
        
        await using var session = sessionFactory.OpenSession();
        
        // Load aggregate
        var aggregate = await session.AggregateStreamAsync<{Entity}Aggregate>(evt.{EntityId});
        if (aggregate is null)
        {
            logger.LogWarning("No {Entity} found for {EntityId}", evt.{EntityId});
            return;
        }
        
        // Perform action
        aggregate.DoSomething(evt);
        
        // Save and publish
        var events = aggregate.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events, ct);
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
No manual registration needed — just make sure `AddMediatR()` scans the assembly.

## Patterns

### Translation (emit event in response to another)
```csharp
// OrderPlaced → StockReserved
var aggregate = await session.AggregateStreamAsync<ProductStockAggregate>(evt.ProductId);
aggregate.Reserve(evt.Quantity, evt.OrderId);

var events = aggregate.UncommittedEvents.ToList();
await session.SaveChangesAsync();
await eventPublisher.PublishAsync(events, ct);
```

### Update state only
```csharp
// StockReserved → Mark order as reserved
var order = await session.AggregateStreamAsync<OrderAggregate>(evt.OrderId);
order.MarkAsReserved();

var events = order.UncommittedEvents.ToList();
await session.SaveChangesAsync();
await eventPublisher.PublishAsync(events, ct);
```

### Side effect only (external call)
```csharp
// OrderShipped → Send email (no aggregate, no events)
await _emailService.SendAsync(evt.CustomerEmail, "Your order shipped!");
// Consider outbox pattern for reliability
```

## Reference

Copy from `ReserveStockOnOrderPlaced.cs` or similar `*On*.cs` files.
