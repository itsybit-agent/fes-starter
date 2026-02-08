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

- Query + Handler + Endpoint in `{ProjectName}.{Module}/Features/`
- ReadModel (if needed)
- Projection (if needed)
- Wires into module endpoints

## Steps

### 0. Detect project name

Find `*.slnx` or `*.sln` — filename is the ProjectName.

### 1. Find a query to copy

Look in `src/{ProjectName}.{Module}/Features/` for an existing query.
If new module, copy from `src/{ProjectName}.Orders/Features/ListOrders.cs`.

### 2. Create the feature file

Copy existing query, create `src/{ProjectName}.{Module}/Features/{QueryName}.cs`:

```csharp
namespace {ProjectName}.{Module}.Features;

// Query
public record {QueryName}Query(
    Guid? {Entity}Id = null  // optional filters
) : IRequest<{QueryName}Result>;

public record {QueryName}Result(IReadOnlyList<{Entity}Dto> Items);

public record {Entity}Dto(
    Guid Id,
    string Status,
    DateTimeOffset CreatedAt
    // add properties
);

// ReadModel (in-memory projection)
public class {QueryName}ReadModel
{
    private readonly List<{Entity}Dto> _items = new();
    
    public IReadOnlyList<{Entity}Dto> Items => _items.AsReadOnly();
    
    public void Apply({Entity}Created e)
    {
        _items.Add(new {Entity}Dto(e.{Entity}Id, "Created", e.CreatedAt));
    }
    
    public void Apply({Entity}Updated e)
    {
        var item = _items.FirstOrDefault(x => x.Id == e.{Entity}Id);
        if (item != null)
        {
            // update item
        }
    }
}

// Handler
public class {QueryName}Handler : IRequestHandler<{QueryName}Query, {QueryName}Result>
{
    private readonly {QueryName}ReadModel _readModel;
    
    public {QueryName}Handler({QueryName}ReadModel readModel)
    {
        _readModel = readModel;
    }
    
    public Task<{QueryName}Result> Handle({QueryName}Query query, CancellationToken ct)
    {
        var items = _readModel.Items;
        
        if (query.{Entity}Id.HasValue)
            items = items.Where(x => x.Id == query.{Entity}Id.Value).ToList();
        
        return Task.FromResult(new {QueryName}Result(items));
    }
}

// Endpoint
public static class {QueryName}Endpoint
{
    public static async Task<IResult> Handle(
        IMediator mediator,
        Guid? {entity}Id = null)
    {
        var query = new {QueryName}Query({entity}Id);
        var result = await mediator.Send(query);
        return Results.Ok(result);
    }
}
```

### 3. Register ReadModel

In `{Module}Module.cs`, `Add{Module}Module()`:

```csharp
services.AddSingleton<{QueryName}ReadModel>();
```

### 4. Add projection handler

Create event handler to update the read model:

```csharp
public class {QueryName}Projector : 
    INotificationHandler<DomainEventNotification<{Entity}Created>>
{
    private readonly {QueryName}ReadModel _readModel;
    
    public Task Handle(DomainEventNotification<{Entity}Created> notification, CancellationToken ct)
    {
        _readModel.Apply(notification.Event);
        return Task.CompletedTask;
    }
}
```

### 5. Wire into module

In `{Module}Module.cs`, add to `Map{Module}Endpoints()`:

```csharp
group.MapGet("/", {QueryName}Endpoint.Handle);
// or
group.MapGet("/{entityId}", {QueryName}Endpoint.Handle);
```

## Reference

Copy from `ListOrders.cs` or `GetStock.cs`.
