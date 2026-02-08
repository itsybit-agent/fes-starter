# scaffold-fes-feature

Scaffolds a complete new feature in FesStarter following CQRS + Event Sourcing patterns.

## Usage

```
/scaffold-fes-feature {context} {feature} {description}
```

## Parameters

- **context**: The bounded context name (e.g., `Orders`, `Inventory`, `Payments`)
- **feature**: The feature name in PascalCase (e.g., `PlaceOrder`, `AdjustStock`)
- **description**: One-line description of what the feature does

## Examples

```
/scaffold-fes-feature Orders PlaceOrder "Create a new order for a product"
/scaffold-fes-feature Payments ProcessRefund "Process a refund for an order"
/scaffold-fes-feature Inventory AdjustStock "Adjust stock quantity for a product"
```

## What It Does

1. **Creates Event** - Domain event in `FesStarter.Events/{Context}/`
2. **Creates Aggregate Method** - Domain logic in aggregate
3. **Creates Feature** - Command/handler/endpoint in single file
4. **Creates Frontend** - API service and component
5. **Wires Everything** - Updates module and routes
6. **Adds Tests** - Integration test template

## Output Structure

```
Backend:
  FesStarter.Events/{Context}/{Feature}Events.cs
  FesStarter.{Context}/Features/{Feature}.cs

Frontend:
  src/app/{context}/{feature}.api.ts
  src/app/{context}/{feature}.component.ts
  src/app/{context}/{feature}.types.ts

Tests:
  tests/FesStarter.Api.Tests/{Feature}Tests.cs

Updated:
  FesStarter.{Context}/{Context}Module.cs
  src/app/{context}/{context}.routes.ts
```

## Requirements

- Project follows FesStarter architecture (CLAUDE.md, SCAFFOLDING.md)
- FileEventStore, MediatR, Angular 19+ setup already in place
- ToastService available for error handling

## Conventions

- Events named `{Feature}Started`, `{Feature}Completed`, etc.
- Commands named `{Feature}Command`
- Handlers named `{Feature}Handler`
- Read models named `{Feature}ReadModel`
- DTOs named `{Feature}Dto`
- Angular components named `{feature}.component.ts`
- All commands are idempotent with `IIdempotencyService`

## Reference

See `SCAFFOLDING.md` for detailed patterns and examples.
