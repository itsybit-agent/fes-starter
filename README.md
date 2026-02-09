# FES Starter

Event-Sourced Modular Monolith Starter with CQRS, Vertical Slices, and Angular.

## Quick Start

```bash
# Install all templates
dotnet new install ./fes-starter
dotnet new install ./fes-starter/templates/fes-module
dotnet new install ./fes-starter/templates/fes-command
dotnet new install ./fes-starter/templates/fes-query
dotnet new install ./fes-starter/templates/fes-automation

# Create a new project
dotnet new fes -n MyApp
cd MyApp

# Run with Aspire
dotnet run --project src/MyApp.AppHost
```

## Available Templates

| Template | Short Name | Description |
|----------|------------|-------------|
| FES Starter | `fes` | Full project with example Orders/Inventory modules |
| FES Module | `fes-module` | New bounded context (project + aggregate + module) |
| FES Command | `fes-command` | Write operation (command + handler + endpoint) |
| FES Query | `fes-query` | Read operation (query + handler + read model + endpoint) |
| FES Automation | `fes-automation` | Cross-context event handler |

## Example: Building ShopQueue from Scratch

Here's how to build a complete app using the templates:

### 1. Create the project

```bash
dotnet new fes -n ShopQueue
cd ShopQueue
```

### 2. Remove example modules (optional)

```bash
rm -rf src/ShopQueue.Orders src/ShopQueue.Inventory
# Edit ShopQueue.slnx to remove references
# Edit src/ShopQueue.Api/Program.cs to remove module registrations
```

### 3. Create your modules

```bash
# Shops module
dotnet new fes-module -n Shops -o src/ShopQueue.Shops --namespace ShopQueue
dotnet sln add src/ShopQueue.Shops/Shops.csproj

# Queues module  
dotnet new fes-module -n Queues -o src/ShopQueue.Queues --namespace ShopQueue
dotnet sln add src/ShopQueue.Queues/Queues.csproj
```

### 4. Add events

Create `src/ShopQueue.Events/Shops/ShopEvents.cs`:
```csharp
public record ShopRegistered(string ShopId, string Name) : IStoreableEvent;
```

Create `src/ShopQueue.Events/Queues/QueueEvents.cs`:
```csharp
public record QueueCreated(string QueueId, string ShopId, string Name) : IStoreableEvent;
public record CustomerJoined(string QueueId, string CustomerId) : IStoreableEvent;
public record CustomerServed(string QueueId, string CustomerId) : IStoreableEvent;
```

### 5. Scaffold features

```bash
# Shops module
dotnet new fes-command -n RegisterShop -o src/ShopQueue.Shops/Features --module Shops --namespace ShopQueue
dotnet new fes-query -n ListShops -o src/ShopQueue.Shops/Features --module Shops --namespace ShopQueue

# Queues module
dotnet new fes-command -n CreateQueue -o src/ShopQueue.Queues/Features --module Queues --namespace ShopQueue
dotnet new fes-command -n JoinQueue -o src/ShopQueue.Queues/Features --module Queues --namespace ShopQueue
dotnet new fes-command -n ServeCustomer -o src/ShopQueue.Queues/Features --module Queues --namespace ShopQueue
dotnet new fes-query -n ListQueues -o src/ShopQueue.Queues/Features --module Queues --namespace ShopQueue
```

### 6. Add cross-context automation

```bash
dotnet new fes-automation -n NotifyNextCustomerOnServed -o src/ShopQueue.Queues/Features --module Queues --namespace ShopQueue
```

### 7. Implement the logic

1. **Edit aggregates** — Add state and command methods
2. **Edit handlers** — Call aggregate methods, save changes
3. **Edit events** — Add required properties
4. **Wire modules** — Register in `Program.cs`

### 8. Run

```bash
dotnet run --project src/ShopQueue.AppHost
```

## Project Structure

```
src/
├── {App}.Events/                   # Shared event contracts
│   ├── {Module}/Events.cs
│   └── ICorrelatedEvent.cs
│
├── {App}.{Module}/                 # Bounded context
│   ├── {Module}Module.cs           # DI + endpoint registration
│   ├── Domain/
│   │   └── {Module}Aggregate.cs    # Write model
│   └── Features/
│       ├── {Command}.cs            # Command + Handler + Endpoint
│       ├── {Query}.cs              # Query + Handler + ReadModel
│       └── {Automation}.cs         # Cross-context handler
│
├── {App}.Api/                      # Composition root
│   ├── Program.cs
│   └── Infrastructure/
│
├── {App}.AppHost/                  # Aspire orchestration
├── {App}.ServiceDefaults/          # Telemetry, health
└── {App}.Web/                      # Angular frontend

templates/                          # Sub-templates
├── fes-module/
├── fes-command/
├── fes-query/
└── fes-automation/
```

## Adding Features (AI-Assisted)

If using Claude/AI, the scaffold skills provide guided generation:

```bash
/scaffold-module Payments "Payment processing"
/scaffold-command Orders CancelOrder "Cancel an order"
/scaffold-query Orders GetOrderDetails "Get order by ID"
/scaffold-automation Inventory ReserveStockOnOrderPlaced
```

See [.claude/skills/README.md](.claude/skills/README.md) for details.

## Uninstall Templates

```bash
dotnet new uninstall fes-starter
dotnet new uninstall fes-module
dotnet new uninstall fes-command
dotnet new uninstall fes-query
dotnet new uninstall fes-automation
```

## License

MIT
