# CLAUDE.md - Architecture Guide

This is a .NET event-sourced application using vertical slice architecture with modular bounded contexts.

## Project Structure

```
src/
├── FesStarter.Events/          # Shared events (contracts between modules)
│   ├── Orders/OrderEvents.cs
│   ├── Inventory/InventoryEvents.cs
│   ├── IEventPublisher.cs
│   ├── ICorrelatedEvent.cs
│   ├── IIdempotentCommand.cs
│   └── DomainEventNotification.cs
│
├── FesStarter.Orders/          # Orders bounded context (module)
│   ├── OrdersModule.cs         # DI + endpoint registration
│   ├── Domain/
│   │   └── OrderAggregate.cs   # Write model (state machine)
│   └── Features/
│       ├── PlaceOrder.cs       # Command + Handler + Endpoint
│       ├── ShipOrder.cs        # Command + Handler + Endpoint
│       ├── ListOrders.cs       # Query + ReadModel + Projections
│       └── MarkOrderReservedOnStockReserved.cs  # Cross-context translation
│
├── FesStarter.Inventory/       # Inventory bounded context (module)
│   ├── InventoryModule.cs
│   ├── Domain/
│   │   └── ProductStockAggregate.cs
│   └── Features/
│       ├── InitializeStock.cs  # Command + Handler + Endpoint
│       ├── GetStock.cs         # Query + ReadModel + Projections
│       ├── ReserveStockOnOrderPlaced.cs   # Cross-context translation
│       └── DeductStockOnOrderShipped.cs   # Cross-context translation
│
├── FesStarter.Api/             # Composition root (thin!)
│   ├── Program.cs              # Wire modules, middleware, run
│   └── Infrastructure/         # Cross-cutting: EventPublisher, ReadModelInitializer,
│                               #   CorrelationId, Idempotency
│
├── FesStarter.AppHost/         # Aspire orchestration
├── FesStarter.ServiceDefaults/ # OpenTelemetry, health checks
└── FesStarter.Web/             # Angular frontend (zoneless, signals)
    ├── src/app/
    │   ├── app.ts              # Root component
    │   ├── app.config.ts       # Providers (zoneless, router, HttpClient)
    │   ├── app.routes.ts       # Lazy-loaded feature routes
    │   ├── orders/             # Orders feature
    │   │   ├── orders.routes.ts
    │   │   ├── orders.api.ts   # HTTP service
    │   │   ├── orders.types.ts # DTOs & command types
    │   │   ├── place-order.component.ts
    │   │   └── order-list.component.ts
    │   ├── inventory/          # Inventory feature
    │   │   ├── inventory.routes.ts
    │   │   ├── inventory.api.ts
    │   │   ├── inventory.types.ts
    │   │   ├── add-product.component.ts
    │   │   └── stock-list.component.ts
    │   └── shared/ui/          # Shared UI components
    ├── src/environments/       # Environment configs
    └── proxy.conf.js           # Dev proxy → API backend
```

## Key Patterns

### 1. One File Per Feature
Each feature contains everything in a single file:
```csharp
// PlaceOrder.cs
public record PlaceOrderCommand(string ProductId, int Quantity, string? IdempotencyKey = null) : IIdempotentCommand;
public record PlaceOrderResponse(string OrderId);

public class PlaceOrderHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher)
{
    public async Task<PlaceOrderResponse> HandleAsync(PlaceOrderCommand command) { ... }
}

public static class PlaceOrderEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/orders", async (HttpRequest request, PlaceOrderCommand cmd, PlaceOrderHandler handler) => ...);
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

### 4. Cross-Context Translations
Cross-context event handlers live **inside the module that owns the reaction**, using MediatR notifications:
```csharp
// ReserveStockOnOrderPlaced.cs (in Inventory module)
public class ReserveStockOnOrderPlacedHandler :
    INotificationHandler<DomainEventNotification<OrderPlaced>>
{
    // When order placed -> reserve inventory
}

// MarkOrderReservedOnStockReserved.cs (in Orders module)
public class MarkOrderReservedOnStockReservedHandler :
    INotificationHandler<DomainEventNotification<StockReserved>>
{
    // When stock reserved -> mark order as placed
}
```

### 5. Read Models + Projections (Colocated)
Read models, projections, query handlers, and endpoints all live in the same feature file:
```csharp
// ListOrders.cs contains:
// - OrderDto (record)
// - OrderReadModel (ConcurrentDictionary, singleton)
// - OrderReadModelProjections (MediatR notification handler)
// - ListOrdersHandler (query handler)
// - ListOrdersEndpoint (HTTP GET)
```

## Angular Frontend (FesStarter.Web)

### Architecture
- **Angular 21** with **zoneless change detection** (`provideZonelessChangeDetection()`)
- **Standalone components** (no NgModules) with inline templates and styles
- **Signals** for all reactive state (required for zoneless -- plain properties won't trigger re-renders)
- **Lazy-loaded routes** per feature module
- **Feature-based folder structure** mirroring backend bounded contexts

### Key Patterns

#### Signals (not plain properties)
```typescript
products = signal<StockDto[]>([]);       // Declare as signal
selectedProductId = signal('');

// Template: call as function
@for (p of products(); track p.productId) { ... }
```

#### Two-Way Binding with Signals
`[(ngModel)]` doesn't bind to signals directly. Use split binding:
```html
<input [ngModel]="selectedProductId()" (ngModelChange)="selectedProductId.set($event)" />
```

#### Component Communication
Command components emit events using `output()`:
```typescript
orderPlaced = output<void>();
// Parent: <app-place-order (orderPlaced)="orderList.loadOrders()" />
```

#### API Services
Each feature has an `*.api.ts` service (`@Injectable({ providedIn: 'root' })`):
```typescript
@Injectable({ providedIn: 'root' })
export class OrdersApi {
  placeOrder(cmd: PlaceOrderCommand) {
    return this.http.post<PlaceOrderResponse>(`${environment.apiUrl}/orders`, cmd);
  }
}
```

#### Modern Template Syntax
Uses Angular 19+ control flow (`@if`, `@for`, `@else`) instead of structural directives.

### Component Types
| Type | Purpose | Example |
|------|---------|---------|
| Page component | Route container, wires children | `OrdersPage` in `orders.routes.ts` |
| Command component | Form/write operation, emits events | `PlaceOrderComponent` |
| Query component | Loads & displays data | `OrderListComponent` |

### Naming Conventions
| Artifact | Convention | Example |
|----------|------------|---------|
| Component file | `feature-name.component.ts` | `place-order.component.ts` |
| API service | `feature.api.ts` | `orders.api.ts` |
| Types/DTOs | `feature.types.ts` | `orders.types.ts` |
| Routes | `feature.routes.ts` | `orders.routes.ts` |
| DTO type | `{Feature}Dto` | `OrderDto`, `StockDto` |
| Command type | `{Action}Command` | `PlaceOrderCommand` |

### Dev Server & Proxy
- `npm start` serves on `http://localhost:4200`
- `proxy.conf.js` forwards `/api` requests to backend (default `http://localhost:5000`)
- Aspire can override the target via `services__api__http__0` env var

## Adding a New Bounded Context

1. **Create project**: `{ProjectName}.{ContextName}/` with `Domain/` and `Features/` subfolders
2. **Add events**: In `{ProjectName}.Events/{ContextName}/`
3. **Create aggregate**: `Domain/{Name}Aggregate.cs`
4. **Create features**: One `.cs` file per command/query in `Features/`
5. **Create module**: `{ContextName}Module.cs` with `Add{X}Module()` and `Map{X}Endpoints()`
6. **Register in Api**: `builder.Services.Add{X}Module()` + `app.Map{X}Endpoints()`
7. **Add MediatR assembly**: Add the module's assembly to `RegisterServicesFromAssemblies()` in `Program.cs`
8. **Add translations** (if needed): As MediatR handlers in the module that owns the reaction

## Adding a New Feature

### Backend
1. Create `{FeatureName}.cs` in the module's `Features/` folder
2. Add command/query record
3. Add handler class
4. Add endpoint static class
5. Register handler in module's `Add{X}Module()`
6. Map endpoint in module's `Map{X}Endpoints()`

### Frontend
1. Add types to `{feature}.types.ts`
2. Add API method to `{feature}.api.ts`
3. Create component (`{feature-name}.component.ts`) using signals for state
4. Wire into page component in `{feature}.routes.ts`

## Tech Stack
- .NET 10 / Minimal APIs
- FileEventStore (file-based event sourcing)
- MediatR (event publishing, projections, cross-context translations)
- Aspire (orchestration, observability)
- Angular 21 (zoneless, signals, standalone components)
- TypeScript (strict mode)
- SCSS (inline in components)
- xUnit + FluentAssertions (testing)

## Build & Test
- Build API: `dotnet build src/FesStarter.Api/`
- Run tests: `dotnet test`
- Do NOT build the `.slnx` directly (Angular entry breaks `dotnet build`)

## Event Model Mapping

| Event Model Element | Code Location |
|---------------------|---------------|
| Event (orange)      | `{ProjectName}.Events/{Context}/{Event}.cs` |
| Command (blue)      | `{ProjectName}.{Context}/Features/{Feature}.cs` |
| Read Model (green)  | `{ProjectName}.{Context}/Features/{Query}.cs` (colocated with projections) |
| Translation (purple)| `{ProjectName}.{Context}/Features/{Translation}.cs` (in reacting module) |
| Swimlane/Context    | `{ProjectName}.{Context}/` project |
