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
// {CommandName}Events.cs
namespace {ProjectName}.Events.{Module};

public record {PastTense}(  // e.g., OrderCancelled
    Guid {Entity}Id,
    DateTimeOffset {PastTense}At
    // add relevant properties
) : IDomainEvent;
```

### 3. Create the feature file

Copy existing command, create `src/{ProjectName}.{Module}/Features/{CommandName}.cs`:

```csharp
namespace {ProjectName}.{Module}.Features;

// Command
public record {CommandName}Command(
    string IdempotencyKey,
    Guid {Entity}Id
    // add properties
) : IRequest<{CommandName}Result>, IIdempotentCommand;

public record {CommandName}Result(bool Success, string? Error = null);

// Handler
public class {CommandName}Handler : IRequestHandler<{CommandName}Command, {CommandName}Result>
{
    // inject IEventStore, IIdempotencyService, IEventPublisher
    
    public async Task<{CommandName}Result> Handle({CommandName}Command cmd, CancellationToken ct)
    {
        // 1. Check idempotency
        // 2. Load aggregate
        // 3. Call aggregate method
        // 4. Save event
        // 5. Publish event
        // 6. Return result
    }
}

// Endpoint
public static class {CommandName}Endpoint
{
    public static async Task<IResult> Handle(
        {CommandName}Request request,
        IMediator mediator,
        HttpContext context)
    {
        var cmd = new {CommandName}Command(
            context.Request.Headers["Idempotency-Key"].FirstOrDefault() ?? "",
            request.{Entity}Id
        );
        
        var result = await mediator.Send(cmd);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
}

public record {CommandName}Request(Guid {Entity}Id);
```

### 4. Add aggregate method (if needed)

In `src/{ProjectName}.{Module}/Domain/{Entity}Aggregate.cs`:

```csharp
public {PastTense} {CommandName}()
{
    // validation
    return new {PastTense}(Id, DateTimeOffset.UtcNow);
}

// In Apply():
case {PastTense} e:
    // update state
    break;
```

### 5. Wire into module

In `{Module}Module.cs`, add to `Map{Module}Endpoints()`:

```csharp
group.MapPost("/{entityId}/{action}", {CommandName}Endpoint.Handle);
```

## Reference

Copy from existing commands in the module.
