# FesStarter - FileEventStore Vertical Slice Starter

A full-stack starter kit demonstrating:
- **Vertical Slice Architecture** - Features organized by use case, not layer
- **Event Sourcing** with [FileEventStore](https://github.com/jocelynenglund/FileBasedEventStore)
- **Angular Frontend** - Matching vertical slices on the client

## Quick Start

**Run the API:**
```bash
cd fes-starter
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
fes-starter/
├── src/
│   ├── FesStarter.Api/               # Backend API
│   │   ├── Features/                 # Vertical slices
│   │   │   ├── CreateTodo/
│   │   │   │   ├── Command.cs
│   │   │   │   ├── Handler.cs
│   │   │   │   └── Endpoint.cs
│   │   │   ├── CompleteTodo/
│   │   │   └── ListTodos/
│   │   └── Domain/
│   │       └── TodoAggregate.cs
│   │
│   └── FesStarter.Web/               # Angular Frontend
│       └── src/app/
│           ├── features/             # Matching vertical slices
│           │   ├── create-todo/
│           │   │   ├── create-todo.component.ts
│           │   │   ├── create-todo.component.html
│           │   │   └── create-todo.component.scss
│           │   └── list-todos/
│           │       └── ...
│           └── shared/
│               ├── api.service.ts    # HTTP client
│               └── api.types.ts      # Shared DTOs
│
└── tests/
    └── FesStarter.Api.Tests/
```

## Vertical Slices - Backend

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
    await using var session = _sessionFactory.OpenSession();
    
    var todo = await session.AggregateStreamOrCreateAsync<TodoAggregate>(id);
    todo.Create(id, command.Title);
    
    await session.SaveChangesAsync();
    
    return new CreateTodoResponse(id);
}
```

## Vertical Slices - Frontend

Each Angular feature matches a backend slice:

```
features/create-todo/
├── create-todo.component.ts    # Component logic
├── create-todo.component.html  # Template
└── create-todo.component.scss  # Styles
```

**Component uses shared API service:**
```typescript
this.api.createTodo({ title: this.title }).subscribe({
  next: () => {
    this.todoCreated.emit();
  }
});
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/todos` | Create a new todo |
| POST | `/api/todos/{id}/complete` | Mark todo as complete |
| GET | `/api/todos` | List all todos |

## Adding a New Feature

### Backend
1. Create folder: `Features/MyFeature/`
2. Add `Command.cs`, `Handler.cs`, `Endpoint.cs`
3. Register handler in `Program.cs`
4. Map endpoint in `Program.cs`

### Frontend
1. Create folder: `features/my-feature/`
2. Add component files
3. Add method to `api.service.ts`
4. Wire into app

## Using as a Template

```bash
# Install as dotnet new template
dotnet new install ./

# Create new project
dotnet new fes-starter -n MyProject
```

## License

MIT
