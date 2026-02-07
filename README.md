# FesStarter - FileEventStore Vertical Slice Starter

A starter kit demonstrating:
- **Vertical Slice Architecture** - Features organized by use case, not layer
- **Event Sourcing** with [FileEventStore](https://github.com/jocelynenglund/FileBasedEventStore)

## Quick Start

```bash
# Clone and run
git clone https://github.com/itsybit-agent/fes-starter.git
cd fes-starter
dotnet run --project src/FesStarter.Api
```

API will be available at `http://localhost:5000`

## Project Structure

```
fes-starter/
├── src/
│   └── FesStarter.Api/
│       ├── Features/                  # Vertical slices
│       │   ├── CreateTodo/
│       │   │   ├── Command.cs
│       │   │   ├── Handler.cs
│       │   │   └── Endpoint.cs
│       │   ├── CompleteTodo/
│       │   │   └── ...
│       │   └── ListTodos/
│       │       ├── Query.cs
│       │       ├── Handler.cs
│       │       ├── Endpoint.cs
│       │       └── TodoReadModel.cs
│       ├── Domain/
│       │   └── TodoAggregate.cs       # Aggregate + Events
│       └── Program.cs
│
└── tests/
    └── FesStarter.Api.Tests/
```

## Vertical Slice Anatomy

Each feature is self-contained:

```
Features/CreateTodo/
├── Command.cs      # Input DTO
├── Handler.cs      # Business logic (uses IEventSession)
└── Endpoint.cs     # HTTP mapping
```

**Handler uses FileEventStore Session:**
```csharp
public async Task<CreateTodoResponse> HandleAsync(CreateTodoCommand command)
{
    var id = Guid.NewGuid().ToString();
    
    await using var session = _sessionFactory.OpenSession();
    
    var todo = await session.AggregateStreamOrCreateAsync<TodoAggregate>(id);
    todo.Create(id, command.Title);
    
    await session.SaveChangesAsync();
    
    return new CreateTodoResponse(id);
}
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/todos` | Create a new todo |
| POST | `/api/todos/{id}/complete` | Mark todo as complete |
| GET | `/api/todos` | List all todos |

## Example Usage

```bash
# Create a todo
curl -X POST http://localhost:5000/api/todos \
  -H "Content-Type: application/json" \
  -d '{"title": "Learn event sourcing"}'

# Complete a todo
curl -X POST http://localhost:5000/api/todos/{id}/complete

# List todos
curl http://localhost:5000/api/todos
```

## Adding a New Feature

1. Create folder: `Features/MyFeature/`
2. Add files:
   - `Command.cs` (or `Query.cs` for reads)
   - `Handler.cs`
   - `Endpoint.cs`
   - Events if needed (or put in Domain/)
3. Register handler in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<MyFeatureHandler>();
   ```
4. Map endpoint in `Program.cs`:
   ```csharp
   app.MapMyFeature();
   ```

## Using as a Template

```bash
# Install as dotnet new template
dotnet new install ./

# Create new project
dotnet new fes-starter -n MyProject
```

## Read Models

The `ListTodos` feature includes a simple in-memory read model. In production:
- Use a proper database (PostgreSQL, etc.)
- Subscribe to events to update read models
- Consider separate read/write databases (CQRS)

## Future Enhancements

- [ ] Angular frontend with matching vertical slices
- [ ] Aspire orchestration
- [ ] Event subscriptions for read model updates
- [ ] Example tests

## License

MIT
