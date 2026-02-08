# FesStarter - Event-Sourced Modular Monolith Starter

A full-stack starter kit demonstrating:
- **CQRS + Event Sourcing** with [FileEventStore](https://github.com/jocelynenglund/FileBasedEventStore)
- **Vertical Slice Architecture** - Features organized by use case, not layer
- **Modular Monolith** - Bounded contexts as separate class library projects
- **Angular Frontend** - Zoneless change detection with signals

## Quick Start

**Run with Aspire (recommended):**
```bash
dotnet run --project src/FesStarter.AppHost
```

**Run the API standalone:**
```bash
dotnet run --project src/FesStarter.Api
# API at http://localhost:5000
```

**Run the Angular app (separate terminal):**
```bash
cd src/FesStarter.Web
npm install
npm start
# App at http://localhost:4200
```

## Project Structure

```
src/
├── FesStarter.Events/              # Shared event contracts between modules
│   ├── Orders/OrderEvents.cs
│   ├── Inventory/InventoryEvents.cs
│   ├── IEventPublisher.cs
│   ├── ICorrelatedEvent.cs
│   ├── IIdempotentCommand.cs
│   └── DomainEventNotification.cs
│
├── FesStarter.Orders/              # Orders bounded context
│   ├── OrdersModule.cs             # DI + endpoint registration
│   ├── Domain/
│   │   └── OrderAggregate.cs       # Write model (state machine)
│   └── Features/
│       ├── PlaceOrder.cs           # Command + Handler + Endpoint
│       ├── ShipOrder.cs            # Command + Handler + Endpoint
│       ├── ListOrders.cs           # Query + ReadModel + Projections
│       └── MarkOrderReservedOnStockReserved.cs  # Cross-context translation
│
├── FesStarter.Inventory/           # Inventory bounded context
│   ├── InventoryModule.cs
│   ├── Domain/
│   │   └── ProductStockAggregate.cs
│   └── Features/
│       ├── InitializeStock.cs      # Command + Handler + Endpoint
│       ├── GetStock.cs             # Query + ReadModel + Projections
│       ├── ReserveStockOnOrderPlaced.cs   # Cross-context translation
│       └── DeductStockOnOrderShipped.cs   # Cross-context translation
│
├── FesStarter.Api/                 # Composition root (thin!)
│   ├── Program.cs                  # Wire modules, middleware, run
│   └── Infrastructure/             # Correlation IDs, Idempotency, ReadModel init
│
├── FesStarter.AppHost/             # Aspire orchestration
├── FesStarter.ServiceDefaults/     # OpenTelemetry, health checks
└── FesStarter.Web/                 # Angular 21 frontend (zoneless, signals)

tests/
├── FesStarter.Orders.Tests/        # Aggregate unit tests
├── FesStarter.Inventory.Tests/     # Aggregate unit tests
└── FesStarter.Api.Tests/           # Integration tests
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/orders` | Place a new order |
| POST | `/api/orders/{orderId}/ship` | Ship an order |
| GET | `/api/orders` | List all orders |
| POST | `/api/products/{productId}/stock` | Initialize product stock |
| GET | `/api/products/{productId}/stock` | Get stock for a product |
| GET | `/api/products/stock` | List all stock |

## Adding a New Feature

### Backend
1. Create `{FeatureName}.cs` in the module's `Features/` folder
2. Add command/query record, handler class, and endpoint static class
3. Register handler in the module's `Add{X}Module()`
4. Map endpoint in the module's `Map{X}Endpoints()`

### Frontend
1. Add types to `{feature}.types.ts`
2. Add API method to `{feature}.api.ts`
3. Create component (`{feature-name}.component.ts`) using signals for state
4. Wire into page component in `{feature}.routes.ts`

## Using as a Template

```bash
# Install as dotnet new template
dotnet new install ./

# Create new project
dotnet new fes-starter -n MyProject
```

## License

MIT
