# FES Starter

Event-Sourced Modular Monolith Starter with CQRS, Vertical Slices, and Angular.

## Installation

```bash
# Install template (once)
dotnet new install itsybit-agent/fes-starter

# Or from local clone
git clone https://github.com/itsybit-agent/fes-starter.git
dotnet new install ./fes-starter
```

## Create a New Project

```bash
# Create project
dotnet new fes -n MyApp
cd MyApp

# Install frontend dependencies
cd src/MyApp.Web
npm install
cd ../..

# Run with Aspire
dotnet run --project src/MyApp.AppHost
```

Opens Aspire dashboard with API + Angular running together.

- API: http://localhost:5000
- Web: http://localhost:4200
- Aspire Dashboard: http://localhost:15000

## What's Included

- **CQRS + Event Sourcing** with [FileEventStore](https://github.com/jocelynenglund/FileBasedEventStore)
- **Vertical Slice Architecture** - Features organized by use case
- **Modular Monolith** - Bounded contexts as separate projects
- **Angular Frontend** - Zoneless change detection with signals
- **Aspire** - Orchestration and observability
- **Scaffold Skills** - AI-assisted code generation

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

## Adding New Features

Use the scaffold skills with Claude:

```bash
# Add a new module (bounded context)
/scaffold-module Payments "Payment processing"

# Add a command (write operation)
/scaffold-command Orders CancelOrder "Cancel an order"

# Add a query (read operation)
/scaffold-query Orders GetOrderDetails "Get order by ID"

# Add an automation (cross-module event handler)
/scaffold-automation Inventory ReserveStockOnOrderPlaced "Reserve stock when order placed"
```

See [.claude/skills/README.md](.claude/skills/README.md) for details.

### Manual approach

**Backend:**
1. Copy an existing feature file from `Features/`
2. Rename and update the command/query/handler
3. Wire into the module

**Frontend:**
1. Copy existing component
2. Update types, API calls, and template

## Uninstall Template

```bash
dotnet new uninstall itsybit-agent/fes-starter
```

## License

MIT
