# scaffold-command

Add a command (write operation) to an existing module.

## Usage

```
/scaffold-command {Module} {CommandName} "{description}"
```

## Examples

```
/scaffold-command Orders CancelOrder "Cancel an order before shipping"
/scaffold-command Payments ProcessRefund "Process a refund for a payment"
```

## What It Creates

- Event in `{ProjectName}.Events/{Module}/`
- Command + Handler + Endpoint in `{ProjectName}.{Module}/Features/`
- Aggregate method (if needed)
- Wires into module endpoints

## Steps

### 0. Detect project name

Find `*.slnx` or `*.sln` — filename is the ProjectName.

### 1. Find a command to copy

Look in `src/{ProjectName}.{Module}/Features/` for an existing command.
If new module, copy from `src/{ProjectName}.Orders/Features/PlaceOrder.cs`.

### 2. Create the event

Copy an existing event, create in `src/{ProjectName}.Events/{Module}/`:

```csharp
// {Module}Events.cs (add to existing file or create new)
namespace {ProjectName}.Events.{Module};

public record {PastTense}(  // e.g., OrderCancelled
    string {Entity}Id,
    DateTime {PastTense}At
    // add relevant properties
) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}
```

### 3. Create the feature file

Copy existing command, create `src/{ProjectName}.{Module}/Features/{CommandName}.cs`:

```csharp
using FileEventStore.Session;
using {ProjectName}.Core.Idempotency;
using {ProjectName}.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace {ProjectName}.{Module};

// Command
public record {CommandName}Command(
    string {Entity}Id,
    // add properties
    string? IdempotencyKey = null
) : IIdempotentCommand
{
    string IIdempotentCommand.IdempotencyKey => IdempotencyKey ?? string.Empty;
}

public record {CommandName}Response(bool Success, string? Error = null);

// Handler
public class {CommandName}Handler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher)
{
    public async Task<{CommandName}Response> HandleAsync({CommandName}Command command)
    {
        await using var session = sessionFactory.OpenSession();
        
        var aggregate = await session.AggregateStreamAsync<{Entity}Aggregate>(command.{Entity}Id);
        if (aggregate is null)
            return new {CommandName}Response(false, "{Entity} not found");
        
        aggregate.{CommandName}();  // Call aggregate method
        
        var events = aggregate.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events);
        
        return new {CommandName}Response(true);
    }
}

// Endpoint
public static class {CommandName}Endpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/{entities}/{entityId}/{action}", async (
            HttpRequest request,
            string {entity}Id,
            {CommandName}Handler handler,
            IIdempotencyService idempotencyService) =>
        {
            var idempotencyKey = request.GetIdempotencyKey();
            var command = new {CommandName}Command({entity}Id, IdempotencyKey: idempotencyKey);
            
            var response = await idempotencyService.GetOrExecuteAsync(
                idempotencyKey ?? "",
                () => handler.HandleAsync(command),
                ct: request.HttpContext.RequestAborted);
            
            return response?.Success == true 
                ? Results.Ok(response) 
                : Results.BadRequest(response);
        })
        .WithName("{CommandName}")
        .WithTags("{Module}");
}
```

### 4. Add aggregate method (if needed)

In `src/{ProjectName}.{Module}/Domain/{Entity}Aggregate.cs`:

```csharp
public {PastTense} {CommandName}()
{
    // validation
    var evt = new {PastTense}(Id, DateTime.UtcNow);
    Apply(evt);
    AddUncommittedEvent(evt);
    return evt;
}

// In Apply():
case {PastTense} e:
    // update state
    break;
```

### 5. Wire into module

In `{Module}Module.cs`, register handler and map endpoint:

```csharp
// In Add{Module}Module():
services.AddScoped<{CommandName}Handler>();

// In Map{Module}Endpoints():
{CommandName}Endpoint.Map(app);
```

## Reference

Copy from existing commands in the module (e.g., `PlaceOrder.cs`).
