# scaffold-feature

Scaffolds a complete new feature following CQRS + Event Sourcing patterns.

Works with any project structure - just specify your naming convention!

## Usage

```
/scaffold-feature {context} {feature} {description} --project-name {ProjectName}
```

## Parameters

- **context**: The bounded context name (e.g., `Orders`, `Inventory`, `Payments`)
- **feature**: The feature name in PascalCase (e.g., `PlaceOrder`, `AdjustStock`)
- **description**: One-line description of what the feature does
- **--project-name**: (Optional) Root namespace prefix. Defaults to detected name from solution

## Examples

```
/scaffold-feature Orders PlaceOrder "Create a new order for a product"
/scaffold-feature Payments ProcessRefund "Process a refund for an order" --project-name Acme
/scaffold-feature Inventory AdjustStock "Adjust stock quantity for a product" --project-name MyApp
```

## What It Does

1. **Creates Event** - Domain event in `{ProjectName}.Events/{Context}/`
2. **Creates Aggregate Method** - Domain logic in aggregate
3. **Creates Feature** - Command/handler/endpoint in single file
4. **Creates Frontend** - API service and component
5. **Wires Everything** - Updates module and routes
6. **Adds Tests** - Integration test template

## Output Structure

```
Backend:
  {ProjectName}.Events/{Context}/{Feature}Events.cs
  {ProjectName}.{Context}/Features/{Feature}.cs

Frontend:
  src/app/{context}/{feature}.api.ts
  src/app/{context}/{feature}.component.ts
  src/app/{context}/{feature}.types.ts

Tests:
  tests/{ProjectName}.Api.Tests/{Feature}Tests.cs

Updated:
  {ProjectName}.{Context}/{Context}Module.cs
  src/app/{context}/{context}.routes.ts
```

## Requirements

- Project follows CQRS + Event Sourcing patterns
- FileEventStore, MediatR, Angular 19+ setup in place
- ToastService available for error handling

## Conventions

- Events named `{Feature}Started`, `{Feature}Completed`, etc.
- Commands named `{Feature}Command`
- Handlers named `{Feature}Handler`
- Read models named `{Feature}ReadModel`
- DTOs named `{Feature}Dto`
- Angular components named `{feature}.component.ts`
- All commands are idempotent with `IIdempotencyService`

## Project Name Detection

Skill automatically detects project name from:
1. `{ProjectName}.sln` or `{ProjectName}.slnx` file
2. First `{ProjectName}.*.csproj` found
3. Falls back to `--project-name` parameter

## Reference

See `SCAFFOLDING.md` for detailed patterns and examples.
