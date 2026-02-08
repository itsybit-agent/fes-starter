# scaffold-query

Add a query (read operation) to an existing module.

## Usage

```
/scaffold-query {Module} {QueryName} "{description}"
```

## Examples

```
/scaffold-query Orders GetOrderDetails "Get order details by ID"
/scaffold-query Payments ListPayments "List all payments with filtering"
```

## What It Creates

- Query handler + Endpoint in `{ProjectName}.{Module}/Features/`
- ReadModel (if needed)
- Projection handlers (if needed)
- Wires into module endpoints

## Steps

### 0. Detect project name

Find `*.slnx` or `*.sln` — filename is the ProjectName.

### 1. Find a query to copy

Look in `src/{ProjectName}.{Module}/Features/` for an existing query.
Copy from `src/{ProjectName}.Orders/Features/ListOrders.cs`.

### 2. Create the feature file

Copy existing query, create `src/{ProjectName}.{Module}/Features/{QueryName}.cs`:

```csharp
using System.Collections.Concurrent;
using FileEventStore;
using {ProjectName}.Events;
using {ProjectName}.Events.{Module};
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.{Module};

// DTO
public record {Entity}Dto(
    string {Entity}Id,
    string Status,
    DateTime CreatedAt
    // add properties
);

// Read Model (in-memory projection)
public class {Entity}ReadModel
{
    private readonly ConcurrentDictionary<string, {Entity}Dto> _items = new();

    public void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case {Entity}Created e:
                _items[e.{Entity}Id] = new {Entity}Dto(e.{Entity}Id, "Created", e.CreatedAt);
                break;
            case {Entity}Updated e:
                if (_items.TryGetValue(e.{Entity}Id, out var item))
                    _items[e.{Entity}Id] = item with { Status = "Updated" };
                break;
        }
    }

    public List<{Entity}Dto> GetAll() => _items.Values.ToList();
    public {Entity}Dto? Get(string id) => _items.GetValueOrDefault(id);
}

// Projections (update read model when events occur)
public class {Entity}ReadModelProjections(
    {Entity}ReadModel readModel,
    ILogger<{Entity}ReadModelProjections> logger) :
    INotificationHandler<DomainEventNotification<{Entity}Created>>,
    INotificationHandler<DomainEventNotification<{Entity}Updated>>
{
    public Task Handle(DomainEventNotification<{Entity}Created> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting {Entity}Created: {Id}", notification.DomainEvent.{Entity}Id);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<{Entity}Updated> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting {Entity}Updated: {Id}", notification.DomainEvent.{Entity}Id);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

// Handler
public class {QueryName}Handler({Entity}ReadModel readModel)
{
    public Task<List<{Entity}Dto>> HandleAsync() => Task.FromResult(readModel.GetAll());
    
    public Task<{Entity}Dto?> HandleAsync(string id) => Task.FromResult(readModel.Get(id));
}

// Endpoint
public static class {QueryName}Endpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/{entities}", async ({QueryName}Handler handler) =>
            Results.Ok(await handler.HandleAsync()))
            .WithName("{QueryName}")
            .WithTags("{Module}");

        app.MapGet("/api/{entities}/{{id}}", async (string id, {QueryName}Handler handler) =>
        {
            var item = await handler.HandleAsync(id);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        })
        .WithName("Get{Entity}")
        .WithTags("{Module}");
    }
}
```

### 3. Register in module

In `{Module}Module.cs`:

```csharp
// In Add{Module}Module():
services.AddSingleton<{Entity}ReadModel>();
services.AddScoped<{QueryName}Handler>();

// In Map{Module}Endpoints():
{QueryName}Endpoint.Map(app);
```

### 4. Initialize read model (optional)

If you need to rebuild read model from existing events on startup,
add a hosted service or use `ReadModelInitializer` pattern from the Api project.

## Reference

Copy from `ListOrders.cs` or `GetStock.cs`.
