# FesStarter Architecture Guide

Complete reference for the event-sourced modular monolith with CQRS.

## Module Organization

Each bounded context follows a consistent vertical slice structure:

```
FesStarter.Orders/
├── OrdersModule.cs                 # DI registration + endpoint mapping
├── Domain/
│   └── OrderAggregate.cs          # Write model (state machine)
├── Features/
│   ├── PlaceOrder.cs              # Command + Handler + Endpoint
│   ├── ShipOrder.cs               # Command + Handler + Endpoint
│   └── ListOrders.cs              # Query Handler + Endpoint + ReadModel
└── Projections/                   # (if complex, separate projection handlers)
```

### Where Do Read Models Live?

**Read models are part of the Query layer and live with their query handlers:**

- `ListOrders.cs` contains both:
  - `OrderReadModel` - the denormalized read state
  - `ListOrdersHandler` - the query handler that reads from the model
  - `ListOrdersEndpoint` - HTTP GET endpoint

**Why this structure?**
1. **Cohesion**: Query logic, read model, and data together
2. **Single responsibility**: Not splitting a query across files
3. **Navigation**: Understanding "how do we query orders?" - look at ListOrders.cs

**Event projections (eventually consistent updates) live in the API project:**

```
FesStarter.Api/
├── Features/Projections/
│   ├── OrderReadModelProjections.cs      # Listens to Order events
│   └── StockReadModelProjections.cs      # Listens to Inventory events
```

This separates infrastructure concerns (MediatR event handling) from business logic.

---

## CQRS Pattern Implementation

### Command Side (Write Model)

```csharp
// Command in feature file
public record PlaceOrderCommand(string ProductId, int Quantity, string? IdempotencyKey = null) : IIdempotentCommand;

// Handler
public class PlaceOrderHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher)
{
    public async Task<PlaceOrderResponse> HandleAsync(PlaceOrderCommand command)
    {
        // 1. Load or create aggregate
        var order = await session.AggregateStreamOrCreateAsync<OrderAggregate>(...);

        // 2. Execute command (changes state, emits events)
        order.Place(command.ProductId, command.Quantity);

        // 3. Persist events (library creates stream ID from aggregate type + identifier)
        var events = order.UncommittedEvents.ToList();
        await session.SaveChangesAsync();

        // 4. Publish events (only!)
        // - EventPublisher decorates with CorrelationId
        // - MediatR broadcasts as DomainEventNotification<T>
        // - Projections subscribe and update read models asynchronously
        await eventPublisher.PublishAsync(events);

        return new PlaceOrderResponse(orderId);
    }
}
```

**Key principle**: Command handlers only publish events. They never directly update read models.

### Query Side (Read Model)

```csharp
// Read model stays in query handler file
public class OrderReadModel
{
    private readonly ConcurrentDictionary<string, OrderDto> _orders = new();

    public void Apply(IStoreableEvent evt) { ... }
    public List<OrderDto> GetAll() => _orders.Values.ToList();
}

// Handler reads from model
public class ListOrdersHandler(OrderReadModel readModel)
{
    public Task<List<OrderDto>> HandleAsync() => Task.FromResult(readModel.GetAll());
}

// Endpoint
public static class ListOrdersEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapGet("/api/orders", async (ListOrdersHandler handler) =>
            Results.Ok(await handler.HandleAsync()));
}
```

### Eventually Consistent Projections

Events automatically update read models via MediatR:

```csharp
// In API/Features/Projections/OrderReadModelProjections.cs
public class OrderReadModelProjections(OrderReadModel readModel, ILogger logger) :
    INotificationHandler<DomainEventNotification<OrderPlaced>>,
    INotificationHandler<DomainEventNotification<OrderStockReserved>>,
    INotificationHandler<DomainEventNotification<OrderShipped>>
{
    public Task Handle(DomainEventNotification<OrderPlaced> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting OrderPlaced: {OrderId}", notification.DomainEvent.OrderId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    // Similar for other events...
}
```

**Benefits:**
- ✅ Read models rebuild from events if needed
- ✅ Multiple read models (same events → different denormalizations)
- ✅ Scales independently (DI container can have multiple projections)
- ✅ Testable (just fire events at read model)

---

## Distributed Tracing with Correlation IDs

### How It Works

```
HTTP Request                        Event Flow                    Event Store
┌─────────────────────┐           ┌──────────────┐              ┌────────────┐
│ X-Correlation-ID:   │           │ OrderPlaced  │              │ Events     │
│ abc-123             │──────────▶ │ CorrId=      │─────────────▶│ CorrId:    │
│                     │           │ abc-123      │              │ abc-123    │
└─────────────────────┘           └──────────────┘              │ CausId: N/A│
                                        │                        └────────────┘
                                        │ publishes
                                        ▼
                                  ┌──────────────────────┐
                                  │ OrderToInventory     │
                                  │ Handler (listening)  │
                                  │ Reads same CorrId    │
                                  │ Creates StockReserved│
                                  │ CorrId=abc-123       │
                                  │ CausId=OrderPlaced   │
                                  └──────────────────────┘
```

### Example: Tracing a Request

```bash
# Client sends request with correlation ID
curl -X POST http://localhost:5000/api/orders \
  -H "X-Correlation-ID: user-req-001" \
  -H "Content-Type: application/json" \
  -d '{"productId":"p1","quantity":5}'

# Response includes correlation ID
# X-Correlation-ID: user-req-001
```

### Logs Show Complete Trace

```
[INF] Request started - CorrelationId=user-req-001, Path=/api/orders
[DBG] Publishing OrderPlaced with CorrelationId=user-req-001, CausationId=none
[DBG] Projecting OrderPlaced to read model: order-123
[DBG] Publishing OrderStockReserved with CorrelationId=user-req-001, CausationId=OrderPlaced
[DBG] Translating OrderPlaced -> StockReserved for Order order-123
[DBG] Publishing StockReserved with CorrelationId=user-req-001, CausationId=OrderPlaced
[DBG] Projecting StockReserved to read model: product-456
[INF] Request completed - CorrelationId=user-req-001, Status=201
```

**All events linked by CorrelationId. Search logs for "user-req-001" to see entire flow.**

### Implementation Details

**1. CorrelationContext (Scoped)**
```csharp
// Stores correlation ID for the current request scope
public class CorrelationContext
{
    public string CorrelationId { get; set; }      // From header or generated
    public string? CausationId { get; set; }       // Set by projections
}
```

**2. CorrelationIdMiddleware**
```csharp
// Extract from headers, generate if missing
var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var value)
    ? value.ToString()
    : Guid.NewGuid().ToString();

// Make available to all handlers
correlationContext.Initialize(correlationId);

// Add to response
context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);
```

**3. CorrelationIdEventPublisher (Decorator)**
```csharp
// When events are published, enrich them with correlation IDs
public class CorrelationIdEventPublisher(IEventPublisher inner, CorrelationContext context)
{
    public async Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct)
    {
        // Clone each event with correlation IDs set
        var enriched = events.Select(e => e switch
        {
            OrderPlaced order => order with
            {
                CorrelationId = context.CorrelationId,
                CausationId = context.CausationId
            },
            // ... similar for all events
        });

        await inner.PublishAsync(enriched, ct);
    }
}
```

### Benefits

- 🔍 **Distributed Tracing**: Follow a user request through multiple services
- 🐛 **Debugging**: Correlate logs, errors, and events
- 📊 **Observability**: Metrics tagged by correlation ID
- 🔗 **Causality**: CausationId shows event dependencies

---

## Idempotency: Safe Retries

### The Problem

```
Client retries due to network timeout
         │
         ├─ POST /api/orders (Idempotency-Key: req-123)
         │  └─ Server creates OrderId: order-1
         │  └─ Network drops before response
         │
         └─ POST /api/orders (Idempotency-Key: req-123) [RETRY]
            └─ Server creates OrderId: order-2 ❌ DUPLICATE!
```

### The Solution

**Commands implement IIdempotentCommand:**

```csharp
public record PlaceOrderCommand(
    string ProductId,
    int Quantity,
    string? IdempotencyKey = null) : IIdempotentCommand
{
    // Implement the required property
    string IIdempotentCommand.IdempotencyKey =>
        IdempotencyKey ?? Guid.NewGuid().ToString();
}
```

**Endpoint extracts key from header:**

```csharp
public static class PlaceOrderEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/orders", async (
            HttpRequest request,
            PlaceOrderCommand command,
            PlaceOrderHandler handler) =>
        {
            // Get idempotency key from header
            var idempotencyKey = request.Headers.TryGetValue("Idempotency-Key", out var value)
                ? value.ToString()
                : null;

            var commandWithKey = command with { IdempotencyKey = idempotencyKey };

            // Handler executes (with caching handled by DI if needed)
            var response = await handler.HandleAsync(commandWithKey);
            return Results.Created($"/api/orders/{response.OrderId}", response);
        });
}
```

**IdempotencyService caches by key:**

```csharp
public class InMemoryIdempotencyService : IIdempotencyService
{
    // Cache: "idempotency:req-123" → PlaceOrderResponse { OrderId: "order-1" }

    public async Task<T?> GetOrExecuteAsync<T>(
        string idempotencyKey,
        Func<Task<T>> executor,
        TimeSpan? expiration = null)
    {
        var cacheKey = $"idempotency:{idempotencyKey}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out T? cached))
        {
            _logger.LogInformation("Idempotency cache hit: {Key}", idempotencyKey);
            return cached;  // Same result as first attempt
        }

        // Execute and cache
        var result = await executor();
        _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
        return result;
    }
}
```

### Idempotency Workflow

```
POST /api/orders with Idempotency-Key: req-123
         │
         ├─ Extract key: "req-123"
         │
         ├─ Check cache: "idempotency:req-123"?
         │  ├─ HIT: Return cached { OrderId: "order-1" }
         │  └─ MISS: Execute handler
         │
         ├─ Handler creates order, publishes events
         │
         └─ Cache response: "idempotency:req-123" → { OrderId: "order-1" }
            └─ Return result

Retry (same key, same response):
POST /api/orders with Idempotency-Key: req-123
         │
         └─ Cache HIT → Return { OrderId: "order-1" } immediately ✅
```

### Client Example

```bash
# First attempt
curl -X POST http://localhost:5000/api/orders \
  -H "Idempotency-Key: user-order-001" \
  -H "Content-Type: application/json" \
  -d '{"productId":"p1","quantity":5}'
# Response: 201 Created { "OrderId": "order-123" }

# Network timeout, client retries with SAME key
curl -X POST http://localhost:5000/api/orders \
  -H "Idempotency-Key: user-order-001" \
  -H "Content-Type: application/json" \
  -d '{"productId":"p1","quantity":5}'
# Response: 201 Created { "OrderId": "order-123" } [Cached result]
# ✅ Same OrderId - no duplicate created!
```

### Deployment Considerations

**For single-server:**
- Use `InMemoryIdempotencyService` (current)
- Cache expires after 24 hours
- Suitable for demo and small deployments

**For distributed deployment:**
```csharp
// Use Redis or distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddScoped<IIdempotencyService, RedisIdempotencyService>();
```

**Cache TTL considerations:**
- Short (1 hour): Risk of duplicate if user retries after timeout
- Long (24 hours): Wastes cache for active operations
- **Default (24 hours)**: Suitable for most scenarios

---

## Stream ID Generation (Automatic)

The FileEventStore library automatically generates stream IDs from the aggregate type and identifier:

```csharp
// Pass just the identifier - library handles the stream ID
var orderId = Guid.NewGuid().ToString();  // e.g., "abc-123"
var order = await session.AggregateStreamOrCreateAsync<OrderAggregate>(orderId);
// Behind the scenes: Stream ID = "OrderAggregate-abc-123"

var productId = "product-456";
var stock = await session.AggregateStreamOrCreateAsync<ProductStockAggregate>(productId);
// Behind the scenes: Stream ID = "ProductStockAggregate-product-456"

// Loading: Use same identifier, library handles the prefix
var loaded = await session.AggregateStreamAsync<OrderAggregate>(orderId);
```

**The pattern is automatic:**
- Aggregate type + identifier → unique stream ID automatically
- Prevents collisions (Order and Stock with same ID are different streams)
- Don't manually add prefixes - let the library generate them
- Load with the same identifier you created with

---

## Testing Read Models

```csharp
[Fact]
public void OrderReadModel_AppliesOrderPlaced()
{
    // Arrange
    var model = new OrderReadModel();
    var @event = new OrderPlaced("order-1", "product-1", 5, DateTime.UtcNow)
    {
        CorrelationId = "test-123"
    };

    // Act
    model.Apply(@event);

    // Assert
    model.Get("order-1").Should().NotBeNull();
    model.Get("order-1")!.Status.Should().Be("Pending");
}
```

**Read models are testable in isolation** - just fire events at them.

---

## Complete Request Flow Example

```
1. Client: POST /api/orders with Idempotency-Key: user-123
           X-Correlation-ID: request-001

2. CorrelationIdMiddleware:
   - Extract/generate CorrelationId
   - Initialize CorrelationContext(CorrelationId="request-001")
   - Add to response headers

3. PlaceOrderEndpoint:
   - Extract IdempotencyKey from header
   - Create command with key

4. PlaceOrderHandler:
   - Load/create OrderAggregate
   - Call aggregate.Place(...)
   - Collect UncommittedEvents

5. FileEventStore:
   - Save events to stream "OrderAggregate-order-123"

6. CorrelationIdEventPublisher (Decorator):
   - Enriches OrderPlaced event:
     * CorrelationId = "request-001"
     * CausationId = null
   - Logs with correlation ID

7. MediatREventPublisher:
   - Broadcasts DomainEventNotification<OrderPlaced>
   - All subscribers notified

8. OrderReadModelProjections (Subscriber):
   - Receives OrderPlaced event
   - Applies to OrderReadModel
   - Logs with correlation ID

9. OrderToInventoryHandler (Subscriber):
   - Translates OrderPlaced → StockReserved
   - Publishes StockReserved event
   - (With CorrelationId="request-001", CausationId=OrderPlaced)

10. StockReadModelProjections:
    - Applies StockReserved to read model

11. IdempotencyService:
    - Cache PlaceOrderResponse { OrderId: "order-123" }
    - Key: "idempotency:user-123"
    - TTL: 24 hours

12. Response:
    - 201 Created
    - X-Correlation-ID: request-001
    - { OrderId: "order-123" }

If client retries (same IdempotencyKey):
    → IdempotencyService returns cached response
    → No new events
    → Same OrderId
```

---

## Summary

| Layer | Location | Example |
|-------|----------|---------|
| **Domain (Write)** | `Orders/Domain/` | OrderAggregate |
| **Features (Commands)** | `Orders/Features/` | PlaceOrder.cs |
| **Read Models** | `Orders/Features/` | OrderReadModel in ListOrders.cs |
| **Queries** | `Orders/Features/` | ListOrdersHandler in ListOrders.cs |
| **Projections** | `Api/Features/Projections/` | OrderReadModelProjections.cs |
| **Events** | `Events/{Context}/` | OrderPlaced.cs (implements ICorrelatedEvent) |
| **Infrastructure** | `Api/Infrastructure/` | CorrelationContext, IdempotencyService |

**Key architectural decisions:**
- ✅ Commands publish events only (no direct read model updates)
- ✅ Read models update asynchronously via event projections
- ✅ All events tracked with CorrelationId for tracing
- ✅ Commands support IIdempotentCommand for safe retries
- ✅ Stream IDs generated automatically by library
