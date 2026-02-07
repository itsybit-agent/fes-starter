# CLAUDE.md - Architecture Guide

This is a .NET event-sourced application using vertical slice architecture with modular bounded contexts.

## Project Structure

```
src/
├── FesStarter.Events/          # Shared events (contracts between modules)
│   ├── Orders/OrderEvents.cs
│   ├── Inventory/InventoryEvents.cs
│   └── IEventPublisher.cs
│
├── FesStarter.Orders/          # Orders bounded context (module)
│   ├── OrderAggregate.cs       # Domain aggregate
│   ├── OrdersModule.cs         # DI + endpoint registration
│   ├── PlaceOrder.cs           # Feature: command + handler + endpoint
│   ├── ShipOrder.cs            # Feature
│   └── ListOrders.cs           # Feature: query + read model
│
├── FesStarter.Inventory/       # Inventory bounded context (module)
│   ├── ProductStockAggregate.cs
│   ├── InventoryModule.cs
│   ├── InitializeStock.cs
│   └── GetStock.cs
│
├── FesStarter.Api/             # Composition root (thin!)
│   ├── Program.cs              # Wire modules, middleware, run
│   ├── Infrastructure/         # Cross-cutting: EventPublisher, ReadModelInitializer
│   └── Features/Translations/  # Cross-context event handlers
│
├── FesStarter.AppHost/         # Aspire orchestration
├── FesStarter.ServiceDefaults/ # OpenTelemetry, health checks
└── FesStarter.Web/             # Angular frontend
```

## Key Patterns

### 1. One File Per Feature
Each feature contains everything in a single file:
```csharp
// PlaceOrder.cs
public record PlaceOrderCommand(string ProductId, int Quantity);
public record PlaceOrderResponse(string OrderId);

public class PlaceOrderHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher, OrderReadModel readModel)
{
    public async Task<PlaceOrderResponse> HandleAsync(PlaceOrderCommand command) { ... }
}

public static class PlaceOrderEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/orders", async (PlaceOrderCommand cmd, PlaceOrderHandler handler) => ...);
}
```

### 2. Module Registration
Each bounded context exposes a module class:
```csharp
// OrdersModule.cs
public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services) { ... }
    public static WebApplication MapOrderEndpoints(this WebApplication app) { ... }
}
```

### 3. Events as Contracts
Events live in the shared `FesStarter.Events` project so modules can reference each other's events without coupling to implementations.

### 4. Stream Translations
Cross-context reactions use MediatR notifications:
```csharp
// OrderToInventory.cs (in Api project)
public class OrderToInventoryHandler : INotificationHandler<DomainEventNotification<OrderPlaced>>
{
    // When order placed -> reserve inventory
}
```

### 5. Read Models
- Singleton in-memory dictionaries (for demo)
- Rebuilt from events on startup via `ReadModelInitializer`
- Use signals in Angular for zoneless change detection

## Adding a New Bounded Context

1. **Create project**: `FesStarter.{ContextName}/`
2. **Add events**: In `FesStarter.Events/{ContextName}/` 
3. **Create aggregate**: `{Name}Aggregate.cs`
4. **Create features**: One `.cs` file per command/query
5. **Create module**: `{ContextName}Module.cs` with `Add{X}Module()` and `Map{X}Endpoints()`
6. **Register in Api**: `builder.Services.Add{X}Module()` + `app.Map{X}Endpoints()`
7. **Add translations** (if needed): In `Api/Features/Translations/`

## Adding a New Feature

1. Create `{FeatureName}.cs` in the module project
2. Add command/query record
3. Add handler class
4. Add endpoint static class
5. Register handler in module's `Add{X}Module()`
6. Map endpoint in module's `Map{X}Endpoints()`

## Tech Stack
- .NET 10 / Minimal APIs
- FileEventStore (file-based event sourcing)
- MediatR (event publishing, translations)
- Aspire (orchestration, observability)
- Angular 19+ (zoneless, signals)

## Event Model Mapping

| Event Model Element | Code Location |
|---------------------|---------------|
| 🟧 Event (orange)   | `FesStarter.Events/{Context}/{Event}.cs` |
| 🟦 Command (blue)   | `FesStarter.{Context}/{Feature}.cs` |
| 📗 Read Model (green) | `FesStarter.{Context}/{Query}.cs` (in same file) |
| ⚙️ Processor (purple) | `FesStarter.Api/Features/Translations/` |
| Swimlane/Context    | `FesStarter.{Context}/` project |
