# FesStarter Scaffolding Guide

This guide provides step-by-step instructions for scaffolding new features and bounded contexts in FesStarter, a CQRS + Event Sourcing application with vertical slice architecture.

## Prerequisites
- Read `CLAUDE.md` for architecture overview
- Understand the event model for your domain
- Familiarize yourself with vertical slice pattern

## Quick Reference

### Event Model → Architecture Mapping
```
Event Model Element    → Code Location                          → Pattern
🟧 Event              → FesStarter.Events/{Context}/{Event}.cs  → Public contract
🟦 Command            → FesStarter.{Context}/{Feature}.cs       → User intent
📗 Read Model         → FesStarter.{Context}/{Query}.cs         → Query response
⚙️ Processor          → FesStarter.Api/Features/Projections/    → Event handler
Swimlane/Context      → FesStarter.{Context}/                   → Bounded context
```

---

## Part 1: Design Event Model

### Step 1: Define Domain Events

Events represent facts that happened. Create them in the shared Events project:

```csharp
// FesStarter.Events/{ContextName}/{EventName}.cs

namespace FesStarter.Events.YourContext;

public record OrderPlaced(
    string OrderId,
    string ProductId,
    int Quantity,
    DateTime Timestamp = default
) : DomainEvent
{
    public OrderPlaced() : this("", "", 0) { }
}

public record OrderShipped(
    string OrderId,
    DateTime ShippedAt = default
) : DomainEvent
{
    public OrderShipped() : this("") { }
}
```

**Key Points:**
- Inherit from `DomainEvent`
- Include all state needed to rebuild aggregates
- Add parameterless constructor for deserialization
- Use immutable records

### Step 2: Create Event Interface (if cross-context)

For events that other contexts need:

```csharp
// In FesStarter.Events/{Context}/

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
}
```

---

## Part 2: Create Bounded Context Module

### Step 1: Create Project Structure

```
src/FesStarter.YourContext/
├── Aggregate.cs              # Domain aggregate (write model)
├── Features/
│   ├── CreateResource.cs     # Feature: command + handler + endpoint
│   ├── UpdateResource.cs
│   └── ListResources.cs      # Feature: query + handler + read model
├── YourContextModule.cs      # DI registration & endpoint mapping
└── FesStarter.YourContext.csproj
```

### Step 2: Create Project File

```xml
<!-- FesStarter.YourContext.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="MediatR" Version="12.4.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../FesStarter.Core/FesStarter.Core.csproj" />
    <ProjectReference Include="../FesStarter.Events/FesStarter.Events.csproj" />
  </ItemGroup>
</Project>
```

### Step 3: Create Aggregate

```csharp
// YourContext/YourAggregate.cs

using FesStarter.Events;

namespace FesStarter.YourContext;

public class YourAggregate : AggregateRoot
{
    public string ResourceId { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int Quantity { get; private set; }

    // Public factory method (no-arg constructor for persistence)
    public YourAggregate() { }

    // Domain logic - emit events, don't mutate state directly
    public void Create(string resourceId, string name, int quantity)
    {
        if (string.IsNullOrEmpty(resourceId)) throw new ArgumentException("ResourceId required");

        ApplyEvent(new ResourceCreated(resourceId, name, quantity));
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity < 0) throw new ArgumentException("Quantity cannot be negative");

        ApplyEvent(new QuantityUpdated(ResourceId, newQuantity));
    }

    // Event handlers - update internal state
    private void ApplyEvent(ResourceCreated evt)
    {
        ResourceId = evt.ResourceId;
        Name = evt.Name;
        Quantity = evt.Quantity;
        AddUncommittedEvent(evt);
    }

    private void ApplyEvent(QuantityUpdated evt)
    {
        Quantity = evt.NewQuantity;
        AddUncommittedEvent(evt);
    }
}
```

---

## Part 3: Create Features (Vertical Slices)

Each feature = one file with command/query + handler + endpoint

### Pattern: Create/Command Feature

```csharp
// YourContext/Features/CreateResource.cs

using FileEventStore.Session;
using FesStarter.Core.Idempotency;
using FesStarter.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FesStarter.YourContext;

// ============ Command ============
public record CreateResourceCommand(
    string ResourceId,
    string Name,
    int Quantity,
    string? IdempotencyKey = null
) : IIdempotentCommand
{
    string IIdempotentCommand.IdempotencyKey =>
        IdempotencyKey ?? string.Empty;
}

public record CreateResourceResponse(string ResourceId);

// ============ Handler ============
public class CreateResourceHandler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher)
{
    public async Task<CreateResourceResponse> HandleAsync(CreateResourceCommand command)
    {
        await using var session = sessionFactory.OpenSession();

        // Get or create aggregate
        var resource = await session.AggregateStreamOrCreateAsync<YourAggregate>(
            command.ResourceId);

        // Execute domain logic
        resource.Create(command.ResourceId, command.Name, command.Quantity);

        // Persist and publish
        var events = resource.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events);

        return new CreateResourceResponse(command.ResourceId);
    }
}

// ============ Endpoint ============
public static class CreateResourceEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/resources", async (
            HttpRequest request,
            CreateResourceCommand command,
            CreateResourceHandler handler,
            IIdempotencyService idempotencyService) =>
        {
            var idempotencyKey = request.GetIdempotencyKey();
            var commandWithKey = command with { IdempotencyKey = idempotencyKey };

            var response = await idempotencyService.GetOrExecuteAsync(
                idempotencyKey ?? "",
                () => handler.HandleAsync(commandWithKey),
                ct: request.HttpContext.RequestAborted);

            if (response is null)
            {
                return Results.Problem(
                    detail: "Failed to create resource.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.Created($"/api/resources/{response.ResourceId}", response);
        })
        .WithName("CreateResource")
        .WithTags("Resources");
}
```

### Pattern: List/Query Feature

```csharp
// YourContext/Features/ListResources.cs

using FileEventStore.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FesStarter.YourContext;

// ============ Query ============
public record ListResourcesQuery;

public record ResourceDto(string ResourceId, string Name, int Quantity);

// ============ Read Model ============
public class ResourceReadModel
{
    private readonly Dictionary<string, ResourceDto> _resources = new();

    public void Add(ResourceDto resource) =>
        _resources[resource.ResourceId] = resource;

    public List<ResourceDto> GetAll() =>
        _resources.Values.ToList();

    public ResourceDto? GetById(string id) =>
        _resources.TryGetValue(id, out var resource) ? resource : null;
}

// ============ Handler ============
public class ListResourcesHandler(ResourceReadModel readModel)
{
    public async Task<List<ResourceDto>> HandleAsync(ListResourcesQuery query) =>
        await Task.FromResult(readModel.GetAll());
}

// ============ Endpoint ============
public static class ListResourcesEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapGet("/api/resources", async (ListResourcesHandler handler) =>
        {
            var resources = await handler.HandleAsync(new ListResourcesQuery());
            return Results.Ok(resources);
        })
        .WithName("ListResources")
        .WithTags("Resources");
}
```

### Pattern: Task Handler (No Return Value)

For commands that don't return data, use the non-generic overload:

```csharp
public class ShipResourceHandler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher)
{
    public async Task HandleAsync(ShipResourceCommand command)
    {
        await using var session = sessionFactory.OpenSession();
        var resource = await session.AggregateStreamOrCreateAsync<YourAggregate>(
            command.ResourceId);

        resource.Ship();

        var events = resource.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events);
    }
}

// In endpoint:
await idempotencyService.GetOrExecuteAsync(
    idempotencyKey ?? "",
    () => handler.HandleAsync(command),
    ct: request.HttpContext.RequestAborted);
```

---

## Part 4: Create Module Registration

```csharp
// YourContext/YourContextModule.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FesStarter.YourContext;

public static class YourContextModule
{
    public static IServiceCollection AddYourContextModule(this IServiceCollection services)
    {
        // Register read models as singletons
        services.AddSingleton<ResourceReadModel>();

        // Register handlers
        services.AddScoped<CreateResourceHandler>();
        services.AddScoped<ListResourcesHandler>();
        services.AddScoped<ShipResourceHandler>();

        return services;
    }

    public static WebApplication MapYourContextEndpoints(this WebApplication app)
    {
        CreateResourceEndpoint.Map(app);
        ListResourcesEndpoint.Map(app);
        ShipResourceEndpoint.Map(app);
        return app;
    }
}
```

---

## Part 5: Wire Into API

### In `Program.cs`:

```csharp
// Register module
builder.Services.AddYourContextModule();

// Map endpoints
app.MapYourContextEndpoints();
```

---

## Part 6: Create Event Projections (Read Models)

For cross-context reactions, create projections in the API project:

```csharp
// Api/Features/Projections/ResourceProjections.cs

using MediatR;
using FesStarter.Events;
using FesStarter.YourContext;

namespace FesStarter.Api.Features.Projections;

public class ResourceCreatedProjection : INotificationHandler<DomainEventNotification<ResourceCreated>>
{
    private readonly ResourceReadModel _readModel;

    public ResourceCreatedProjection(ResourceReadModel readModel)
    {
        _readModel = readModel;
    }

    public async Task Handle(DomainEventNotification<ResourceCreated> notification, CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        _readModel.Add(new ResourceDto(evt.ResourceId, evt.Name, evt.Quantity));
        await Task.CompletedTask;
    }
}
```

---

## Part 7: Frontend (Angular)

### Step 1: Create Types

```typescript
// src/app/resources/resources.types.ts

export interface ResourceDto {
  resourceId: string;
  name: string;
  quantity: number;
}

export interface CreateResourceCommand {
  resourceId: string;
  name: string;
  quantity: number;
}

export interface CreateResourceResponse {
  resourceId: string;
}
```

### Step 2: Create API Service

```typescript
// src/app/resources/resources.api.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ResourceDto, CreateResourceCommand, CreateResourceResponse } from './resources.types';

@Injectable({ providedIn: 'root' })
export class ResourcesApi {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  createResource(command: CreateResourceCommand, idempotencyKey?: string): Observable<CreateResourceResponse> {
    const key = idempotencyKey || crypto.randomUUID();
    return this.http.post<CreateResourceResponse>(
      `${this.baseUrl}/resources`,
      command,
      { headers: { 'Idempotency-Key': key } }
    );
  }

  listResources(): Observable<ResourceDto[]> {
    return this.http.get<ResourceDto[]>(`${this.baseUrl}/resources`);
  }
}
```

### Step 3: Create Components

```typescript
// src/app/resources/create-resource.component.ts

import { Component, signal, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ResourcesApi } from './resources.api';
import { ToastService } from '../shared/toast.service';

@Component({
  selector: 'app-create-resource',
  standalone: true,
  imports: [FormsModule],
  template: `
    <h3>Create Resource</h3>
    <div class="form">
      <input type="text" [ngModel]="resourceId()" (ngModelChange)="resourceId.set($event)" placeholder="ID">
      <input type="text" [ngModel]="name()" (ngModelChange)="name.set($event)" placeholder="Name">
      <input type="number" [ngModel]="quantity()" (ngModelChange)="quantity.set($event)" placeholder="Qty">
      <button (click)="create()" [disabled]="!resourceId() || !name()">Create</button>
    </div>
  `
})
export class CreateResourceComponent {
  resourceId = signal('');
  name = signal('');
  quantity = signal(0);

  resourceCreated = output<void>();

  constructor(
    private api: ResourcesApi,
    private toast: ToastService
  ) {}

  create() {
    if (!this.resourceId() || !this.name()) return;
    this.api.createResource({
      resourceId: this.resourceId(),
      name: this.name(),
      quantity: this.quantity()
    }).subscribe({
      next: () => {
        this.resourceId.set('');
        this.name.set('');
        this.quantity.set(0);
        this.resourceCreated.emit();
        this.toast.success('Resource created');
      },
      error: err => this.toast.error('Failed to create resource')
    });
  }
}
```

### Step 4: Create Routes

```typescript
// src/app/resources/resources.routes.ts

import { Routes } from '@angular/router';
import { ResourcesPageComponent } from './resources-page.component';

export const resourcesRoutes: Routes = [
  {
    path: '',
    component: ResourcesPageComponent
  }
];
```

---

## Checklist: Scaffolding a New Feature

### Backend
- [ ] Create event(s) in `FesStarter.Events/{Context}/`
- [ ] Create aggregate in `FesStarter.{Context}/`
- [ ] Create feature file with command/query + handler + endpoint
- [ ] Update module's `Add{X}Module()` and `Map{X}Endpoints()`
- [ ] Wire into `Program.cs`
- [ ] Add projections if cross-context (in `Api/Features/Projections/`)
- [ ] Write tests

### Frontend
- [ ] Create `.types.ts` file with DTOs
- [ ] Create `.api.ts` service file
- [ ] Create component(s)
- [ ] Create routes
- [ ] Wire into `app.routes.ts`
- [ ] Add error handling with `ToastService`

### Testing
- [ ] Backend: Integration tests in `tests/FesStarter.Api.Tests/`
- [ ] Frontend: E2E tests (optional)

---

## Common Patterns

### Pattern: Idempotent Command
All write commands should be idempotent using `IIdempotencyService`:

```csharp
await idempotencyService.GetOrExecuteAsync(
    idempotencyKey ?? "",
    () => handler.HandleAsync(command),
    ct: request.HttpContext.RequestAborted);
```

### Pattern: Error Handling (Frontend)
Always handle errors with toast notifications:

```typescript
.subscribe({
  next: (data) => { /* success */ },
  error: (err) => this.toast.error('Operation failed')
});
```

### Pattern: Stream ID Generation
Stream IDs are auto-generated from aggregate type and identifier:

```csharp
var resource = await session.AggregateStreamOrCreateAsync<YourAggregate>(resourceId);
// Stream ID: YourAggregate-{resourceId}
```

### Pattern: Correlation IDs
Events automatically carry correlation IDs for distributed tracing. Check response headers for `X-Correlation-ID`.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Events not being saved | Call `await session.SaveChangesAsync()` before publishing |
| Read model not updating | Ensure projection handler is registered and subscribes to correct event |
| Idempotency not working | Register service as **Singleton**, not Scoped |
| Build fails with project reference | Use correct relative path: `..\\FesStarter.Events\\` |
| Test timeout | Ensure projections/read model initialization completes |

---

## File Templates

### Quick Copy-Paste Templates

**Feature File Template:**
```csharp
using FileEventStore.Session;
using FesStarter.Core.Idempotency;
using FesStarter.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FesStarter.YourContext;

public record YourCommand(string Id, string? IdempotencyKey = null) : IIdempotentCommand
{
    string IIdempotentCommand.IdempotencyKey => IdempotencyKey ?? string.Empty;
}

public record YourResponse(string Id);

public class YourHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher)
{
    public async Task<YourResponse> HandleAsync(YourCommand command)
    {
        await using var session = sessionFactory.OpenSession();
        var aggregate = await session.AggregateStreamOrCreateAsync<YourAggregate>(command.Id);

        // TODO: Domain logic

        var events = aggregate.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events);

        return new YourResponse(command.Id);
    }
}

public static class YourEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/your", async (
            HttpRequest request,
            YourCommand command,
            YourHandler handler,
            IIdempotencyService idempotencyService) =>
        {
            var idempotencyKey = request.GetIdempotencyKey();
            var commandWithKey = command with { IdempotencyKey = idempotencyKey };

            var response = await idempotencyService.GetOrExecuteAsync(
                idempotencyKey ?? "",
                () => handler.HandleAsync(commandWithKey),
                ct: request.HttpContext.RequestAborted);

            return response is null
                ? Results.Problem("Operation failed", statusCode: 500)
                : Results.Created($"/api/your/{response.Id}", response);
        })
        .WithName("Your")
        .WithTags("Your");
}
```

---

## References
- `CLAUDE.md` - Architecture overview
- `src/FesStarter.Orders/` - Real example (Orders context)
- `src/FesStarter.Inventory/` - Real example (Inventory context)
