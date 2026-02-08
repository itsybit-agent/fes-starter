# FesStarter Feature Scaffolding Implementation

This document explains how to use the `scaffold-fes-feature` skill to generate complete features.

## How It Works

When you invoke:
```
/scaffold-fes-feature Orders PlaceOrder "Create a new order for a product"
```

The skill generates:

### 1. **Events** (FesStarter.Events/{Context})
```csharp
// OrderPlaced event capturing intent
public record OrderPlaced(
    string OrderId,
    string ProductId,
    int Quantity,
    DateTime Timestamp = default
) : DomainEvent
{
    public OrderPlaced() : this("", "", 0) { }
}
```

### 2. **Aggregate Method** (FesStarter.{Context})
Adds domain logic to the aggregate:
```csharp
public void Place(string orderId, string productId, int quantity)
{
    if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
    ApplyEvent(new OrderPlaced(orderId, productId, quantity));
}

private void ApplyEvent(OrderPlaced evt)
{
    OrderId = evt.OrderId;
    ProductId = evt.ProductId;
    Quantity = evt.Quantity;
    AddUncommittedEvent(evt);
}
```

### 3. **Feature File** (FesStarter.{Context}/Features)
Complete command handler + endpoint:
```csharp
// Command
public record PlaceOrderCommand(
    string ProductId,
    int Quantity,
    string? IdempotencyKey = null
) : IIdempotentCommand { ... }

// Handler
public class PlaceOrderHandler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher)
{
    public async Task<PlaceOrderResponse> HandleAsync(PlaceOrderCommand command)
    { ... }
}

// Endpoint
public static class PlaceOrderEndpoint
{
    public static void Map(WebApplication app) => ...
}
```

### 4. **Read Model** (same feature file)
```csharp
public class OrderReadModel
{
    private readonly Dictionary<string, OrderDto> _orders = new();
    public void Add(OrderDto order) => _orders[order.OrderId] = order;
    public List<OrderDto> GetAll() => _orders.Values.ToList();
}
```

### 5. **Angular API Service** (src/app/{context})
```typescript
@Injectable({ providedIn: 'root' })
export class OrdersApi {
  placeOrder(command: PlaceOrderCommand): Observable<PlaceOrderResponse> {
    const key = crypto.randomUUID();
    return this.http.post<PlaceOrderResponse>(
      `${this.baseUrl}/orders`,
      command,
      { headers: { 'Idempotency-Key': key } }
    );
  }
}
```

### 6. **Angular Component**
```typescript
export class PlaceOrderComponent {
  selectedProductId = signal('');
  quantity = signal(1);

  constructor(
    private ordersApi: OrdersApi,
    private toast: ToastService
  ) {}

  placeOrder() {
    this.ordersApi.placeOrder({
      productId: this.selectedProductId(),
      quantity: this.quantity()
    }).subscribe({
      next: () => this.toast.success('Order placed'),
      error: () => this.toast.error('Failed to place order')
    });
  }
}
```

### 7. **Module Wiring**
Updates `{Context}Module.cs`:
```csharp
public static IServiceCollection Add{Context}Module(this IServiceCollection services)
{
    services.AddSingleton<OrderReadModel>();
    services.AddScoped<PlaceOrderHandler>();
    return services;
}

public static WebApplication Map{Context}Endpoints(this WebApplication app)
{
    PlaceOrderEndpoint.Map(app);
    return app;
}
```

### 8. **Tests**
Integration tests:
```csharp
[Fact]
public async Task PlaceOrder_WithValidCommand_ReturnsOrderId()
{
    // Arrange
    var command = new PlaceOrderCommand("product-1", 5);

    // Act
    var response = await handler.HandleAsync(command);

    // Assert
    Assert.NotNull(response.OrderId);
}
```

## Manual Steps (if not automated)

If the skill doesn't fully automate:

### Step 1: Create Event
```bash
# File: src/FesStarter.Events/{Context}/{Feature}Events.cs
cat > src/FesStarter.Events/Orders/OrderEvents.cs << 'EOF'
namespace FesStarter.Events.Orders;

public record OrderPlaced(
    string OrderId,
    string ProductId,
    int Quantity,
    DateTime Timestamp = default
) : DomainEvent
{
    public OrderPlaced() : this("", "", 0) { }
}
EOF
```

### Step 2: Create Feature
Copy template from SCAFFOLDING.md and fill in:
- Event names
- Command/response types
- Handler logic
- Endpoint route

### Step 3: Update Module
Add handler registration:
```csharp
services.AddScoped<PlaceOrderHandler>();
```

Add endpoint mapping:
```csharp
PlaceOrderEndpoint.Map(app);
```

### Step 4: Create Frontend Service
```bash
# File: src/FesStarter.Web/src/app/orders/place-order.api.ts
```

### Step 5: Create Component
```bash
# File: src/FesStarter.Web/src/app/orders/place-order.component.ts
```

### Step 6: Update Routes
Add to `orders.routes.ts`:
```typescript
{
  path: 'place',
  component: PlaceOrderComponent
}
```

### Step 7: Build & Test
```bash
# Backend
dotnet build src/FesStarter.Api/

# Frontend
cd src/FesStarter.Web
npm run build

# Tests
dotnet test tests/FesStarter.Api.Tests/
```

## Skill Invocation Examples

### Example 1: Create Order Feature
```
/scaffold-fes-feature Orders PlaceOrder "Create a new order for a product with quantity"
```

Generated files:
- `FesStarter.Events/Orders/PlaceOrderEvents.cs`
- `FesStarter.Orders/Features/PlaceOrder.cs`
- `src/FesStarter.Web/src/app/orders/place-order.api.ts`
- `src/FesStarter.Web/src/app/orders/place-order.component.ts`

### Example 2: Create Refund Feature
```
/scaffold-fes-feature Payments ProcessRefund "Process refund for an order with amount"
```

### Example 3: Create Stock Adjustment
```
/scaffold-fes-feature Inventory AdjustStock "Adjust product stock quantity for inventory management"
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Handler not found" | Make sure `AddXxxModule()` includes all handlers |
| "Endpoint not registered" | Check `MapXxxEndpoints()` includes all endpoints |
| "Read model returns empty" | Ensure projection handler is registered in API |
| "Build fails" | Run `dotnet clean && dotnet build` |
| "Tests fail" | Check event stream IDs match aggregate IDs |

## Next Steps After Scaffolding

1. **Review Generated Code** - Check imports, namespaces, types
2. **Add Domain Logic** - Fill in aggregate validation rules
3. **Wire Events** - Ensure projections handle all new events
4. **Test** - Write additional domain tests
5. **Frontend** - Customize component templates and styles
6. **Documentation** - Update API docs if needed

## Extending the Skill

To add new patterns (e.g., saga handlers, external service integrations):

1. Update SCAFFOLDING.md with pattern
2. Update this file with template
3. Extend the skill to generate pattern files
