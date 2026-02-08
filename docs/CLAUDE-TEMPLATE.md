# CLAUDE.md - Event-Sourced Modular Monolith Guide

> **Template:** Drop this file into any new repo to guide Claude in applying these architectural patterns.
> Replace `{ProjectName}` with your project name (e.g., `OrderFlow`, `InventoryHub`).

This is a .NET event-sourced application using vertical slice architecture with modular bounded contexts.

## Project Structure

```
src/
├── {ProjectName}.Events/           # Shared events (contracts between modules)
│   ├── {BoundedContext1}/{Context1}Events.cs
│   ├── {BoundedContext2}/{Context2}Events.cs
│   ├── IEventPublisher.cs
│   ├── IIdempotentCommand.cs
│   └── DomainEventNotification.cs
│
├── {ProjectName}.{BoundedContext1}/  # Bounded context module
│   ├── Domain/
│   │   └── {Aggregate}Aggregate.cs   # Domain aggregate (state machine)
│   ├── Features/
│   │   ├── {Command1}.cs             # Feature: command + handler + endpoint + response
│   │   ├── {Command2}.cs             # Feature: another command
│   │   ├── {Query1}.cs               # Feature: query + read model + handler + endpoint
│   │   └── {Translation}.cs          # Event translation handler (cross-context)
│   └── {BoundedContext1}Module.cs    # DI + endpoint registration
│
├── {ProjectName}.{BoundedContext2}/  # Another bounded context module
│   ├── Domain/
│   │   └── {Aggregate}Aggregate.cs
│   ├── Features/
│   │   ├── {Feature}.cs
│   │   └── {Translation}.cs
│   └── {BoundedContext2}Module.cs
│
├── {ProjectName}.Core/              # Shared infrastructure (optional)
│   └── Idempotency/
│       ├── IIdempotencyService.cs
│       └── InMemoryIdempotencyService.cs
│
├── {ProjectName}.Api/               # Composition root (thin!)
│   ├── Program.cs                   # Wire modules, middleware, run
│   └── Infrastructure/              # Cross-cutting: EventPublisher, CorrelationId, etc.
│
├── {ProjectName}.Web/               # Angular frontend (mirrors backend structure)
│   └── src/app/
│       ├── app.ts                   # Root component
│       ├── app.config.ts            # Zoneless config, providers
│       ├── app.routes.ts            # Lazy-loaded routes per context
│       ├── {context1}/              # Bounded context folder
│       │   ├── {context1}.api.ts    # HTTP client service
│       │   ├── {context1}.types.ts  # DTOs matching backend
│       │   ├── {context1}.routes.ts # Context routes + page component
│       │   ├── {feature1}.component.ts  # Feature component (signals)
│       │   └── {feature2}.component.ts
│       └── {context2}/              # Another bounded context
│           ├── {context2}.api.ts
│           ├── {context2}.types.ts
│           ├── {context2}.routes.ts
│           └── {feature}.component.ts
│
tests/
├── {ProjectName}.{Context}.Tests/
│   └── Domain/{Aggregate}Tests.cs   # Unit tests for aggregates
└── {ProjectName}.Api.Tests/
    ├── {Context}IntegrationTests.cs # HTTP integration tests
    └── CrossContextTranslationTests.cs
```

---

## Core Patterns

### 1. One File Per Feature (Vertical Slice)

Each feature contains everything in a single file - command, response, handler, and endpoint:

```csharp
// Features/{FeatureName}.cs

// 1. Command record (immutable)
public record {FeatureName}Command(
    string Param1,
    int Param2,
    string? IdempotencyKey = null) : IIdempotentCommand
{
    string IIdempotentCommand.IdempotencyKey => IdempotencyKey ?? string.Empty;
}

// 2. Response record
public record {FeatureName}Response(string ResultId);

// 3. Handler class (inject dependencies via constructor)
public class {FeatureName}Handler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher)
{
    public async Task<{FeatureName}Response> HandleAsync({FeatureName}Command command)
    {
        var id = Guid.NewGuid().ToString();
        
        await using var session = sessionFactory.OpenSession();
        var aggregate = await session.AggregateStreamOrCreateAsync<{Aggregate}Aggregate>(id);
        
        aggregate.DoAction(command.Param1, command.Param2);
        
        var events = aggregate.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events);
        
        return new {FeatureName}Response(id);
    }
}

// 4. Endpoint static class
public static class {FeatureName}Endpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/{resource}", async (
            HttpRequest request,
            {FeatureName}Command command,
            {FeatureName}Handler handler,
            IIdempotencyService idempotencyService) =>
        {
            var idempotencyKey = request.GetIdempotencyKey();
            var commandWithKey = command with { IdempotencyKey = idempotencyKey };
            
            var response = await idempotencyService.GetOrExecuteAsync(
                idempotencyKey ?? "",
                () => handler.HandleAsync(commandWithKey),
                ct: request.HttpContext.RequestAborted);
            
            return response is null
                ? Results.Problem("Failed to execute", statusCode: 500)
                : Results.Created($"/api/{resource}/{response.ResultId}", response);
        })
        .WithName("{FeatureName}")
        .WithTags("{BoundedContext}");
}
```

### 2. Module Registration Pattern

Each bounded context exposes a module class with two extension methods:

```csharp
// {BoundedContext}Module.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.{BoundedContext};

public static class {BoundedContext}Module
{
    public static IServiceCollection Add{BoundedContext}Module(this IServiceCollection services)
    {
        // Read models (singletons for in-memory)
        services.AddSingleton<{Entity}ReadModel>();
        services.AddSingleton<{Entity}ReadModelProjections>();
        
        // Handlers (scoped - per request)
        services.AddScoped<{Command1}Handler>();
        services.AddScoped<{Command2}Handler>();
        services.AddScoped<{Query}Handler>();
        
        // Translation handlers (cross-context event reactions)
        services.AddScoped<{Translation}Handler>();
        
        return services;
    }
    
    public static WebApplication Map{BoundedContext}Endpoints(this WebApplication app)
    {
        {Command1}Endpoint.Map(app);
        {Command2}Endpoint.Map(app);
        {Query}Endpoint.Map(app);
        return app;
    }
}
```

**In Program.cs:**
```csharp
// Register modules
builder.Services.Add{BoundedContext1}Module();
builder.Services.Add{BoundedContext2}Module();

// ... app building ...

// Map endpoints
app.Map{BoundedContext1}Endpoints();
app.Map{BoundedContext2}Endpoints();
```

### 3. Domain Aggregate Pattern

Aggregates are state machines that emit events:

```csharp
// Domain/{Name}Aggregate.cs
using FileEventStore;
using FileEventStore.Aggregates;
using {ProjectName}.Events.{BoundedContext};

namespace {ProjectName}.{BoundedContext};

public class {Name}Aggregate : Aggregate
{
    // State properties (private setters)
    public string SomeProperty { get; private set; } = "";
    public int Counter { get; private set; }
    public {Name}Status Status { get; private set; } = {Name}Status.Initial;
    
    // Command methods (validate + emit events)
    public void DoAction(string param1, int param2)
    {
        // Validation
        if (string.IsNullOrEmpty(Id))
            throw new InvalidOperationException("Not initialized");
        
        if (Status != {Name}Status.Ready)
            throw new InvalidOperationException($"Cannot do action in status {Status}");
        
        if (param2 <= 0)
            throw new ArgumentException("Must be positive", nameof(param2));
        
        // Emit event (don't mutate state directly!)
        Emit(new {Name}ActionDone(Id, param1, param2, DateTime.UtcNow));
    }
    
    // Apply events to update state
    protected override void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case {Name}Created e:
                Id = e.EntityId;
                SomeProperty = e.Property;
                Status = {Name}Status.Ready;
                break;
            case {Name}ActionDone e:
                Counter += e.Amount;
                break;
            // Handle all events this aggregate can emit
        }
    }
}

public enum {Name}Status
{
    Initial,
    Ready,
    Completed
}
```

**Key principles:**
- Aggregates ONLY emit events via `Emit()`
- State changes happen ONLY in `Apply()`
- Command methods validate, then emit
- Events are immutable records

### 4. Event Definition Pattern

Events live in the shared Events project. Use plain records:

```csharp
// Events/{BoundedContext}/{Context}Events.cs
using FileEventStore;

namespace {ProjectName}.Events.{BoundedContext};

// For monoliths: plain records are sufficient
// ICorrelatedEvent adds CorrelationId/CausationId for distributed tracing
public record {Entity}Created(
    string EntityId,
    string Property,
    DateTime CreatedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}

public record {Entity}ActionDone(
    string EntityId,
    string Details,
    int Amount,
    DateTime ActionedAt) : ICorrelatedEvent
{
    public string TimestampUtc { get; set; } = "";
    public string CorrelationId { get; init; } = "";
    public string? CausationId { get; init; }
}
```

**Naming conventions:**
- Past tense: `OrderPlaced`, `StockReserved`, `PaymentReceived`
- Include entity ID and timestamp
- Include relevant data (but not too much - events are stored forever)

### 5. Cross-Context Translation Pattern

When one context needs to react to another context's events:

```csharp
// Features/{ReactionName}On{TriggerEvent}.cs
using FileEventStore.Session;
using {ProjectName}.Events;
using {ProjectName}.Events.{OtherContext};
using MediatR;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.{BoundedContext};

/// <summary>
/// Translates {TriggerEvent} event (from {OtherContext}) to action in {BoundedContext}.
/// </summary>
public class {ReactionName}On{TriggerEvent}Handler(
    IEventSessionFactory sessionFactory,
    IEventPublisher eventPublisher,
    ILogger<{ReactionName}On{TriggerEvent}Handler> logger) :
    INotificationHandler<DomainEventNotification<{TriggerEvent}>>
{
    public async Task Handle(
        DomainEventNotification<{TriggerEvent}> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;
        logger.LogInformation("Reacting to {EventType}: {EntityId}",
            nameof({TriggerEvent}), evt.EntityId);
        
        await using var session = sessionFactory.OpenSession();
        
        var aggregate = await session.AggregateStreamAsync<{Aggregate}Aggregate>(evt.RelatedId);
        if (aggregate == null)
        {
            logger.LogWarning("Aggregate not found: {Id}", evt.RelatedId);
            return;
        }
        
        aggregate.React(evt.SomeData);
        
        var events = aggregate.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events, ct);
    }
}
```

**Key points:**
- Implement `INotificationHandler<DomainEventNotification<TEvent>>`
- Register in the module that OWNS the action (not where event originates)
- Handle missing aggregates gracefully (log, don't throw)

### 6. Read Model + Query Pattern

Read models live with their query handlers:

```csharp
// Features/{Query}.cs
using System.Collections.Concurrent;
using FileEventStore;
using {ProjectName}.Events;
using {ProjectName}.Events.{BoundedContext};
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.{BoundedContext};

// 1. DTO for external representation
public record {Entity}Dto(
    string Id,
    string Name,
    string Status,
    DateTime CreatedAt);

// 2. Read model (in-memory, eventually consistent)
public class {Entity}ReadModel
{
    private readonly ConcurrentDictionary<string, {Entity}Dto> _items = new();
    
    public void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case {Entity}Created e:
                _items[e.EntityId] = new {Entity}Dto(
                    e.EntityId, e.Property, "Active", e.CreatedAt);
                break;
            case {Entity}Updated e:
                if (_items.TryGetValue(e.EntityId, out var existing))
                    _items[e.EntityId] = existing with { Name = e.NewName };
                break;
            // Handle relevant events
        }
    }
    
    public List<{Entity}Dto> GetAll() => _items.Values.ToList();
    public {Entity}Dto? Get(string id) => _items.GetValueOrDefault(id);
}

// 3. Projections (MediatR handler that updates read model)
public class {Entity}ReadModelProjections(
    {Entity}ReadModel readModel,
    ILogger<{Entity}ReadModelProjections> logger) :
    INotificationHandler<DomainEventNotification<{Entity}Created>>,
    INotificationHandler<DomainEventNotification<{Entity}Updated>>
{
    public Task Handle(DomainEventNotification<{Entity}Created> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting {EventType}: {Id}",
            nameof({Entity}Created), notification.DomainEvent.EntityId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }
    
    public Task Handle(DomainEventNotification<{Entity}Updated> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting {EventType}: {Id}",
            nameof({Entity}Updated), notification.DomainEvent.EntityId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

// 4. Query handler
public class List{Entities}Handler({Entity}ReadModel readModel)
{
    public Task<List<{Entity}Dto>> HandleAsync() => Task.FromResult(readModel.GetAll());
}

// 5. Endpoint
public static class List{Entities}Endpoint
{
    public static void Map(WebApplication app) =>
        app.MapGet("/api/{entities}", async (List{Entities}Handler handler) =>
            Results.Ok(await handler.HandleAsync()))
        .WithName("List{Entities}")
        .WithTags("{BoundedContext}");
}
```

### 7. Idempotency Pattern

For safe command retries:

```csharp
// Core/Idempotency/IIdempotencyService.cs
namespace {ProjectName}.Core.Idempotency;

public interface IIdempotencyService
{
    Task<T?> GetOrExecuteAsync<T>(
        string idempotencyKey,
        Func<Task<T>> executor,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}
```

```csharp
// Commands implement IIdempotentCommand
public record MyCommand(string Data, string? IdempotencyKey = null) : IIdempotentCommand
{
    string IIdempotentCommand.IdempotencyKey => IdempotencyKey ?? string.Empty;
}

// Endpoints use the service
var response = await idempotencyService.GetOrExecuteAsync(
    idempotencyKey ?? "",
    () => handler.HandleAsync(command),
    ct: cancellationToken);
```

**Extract idempotency key from headers:**
```csharp
public static class IdempotencyExtensions
{
    public static string? GetIdempotencyKey(this HttpRequest request) =>
        request.Headers.TryGetValue("Idempotency-Key", out var value)
            ? value.ToString()
            : null;
}
```

---

## Infrastructure Setup

### Program.cs Template

```csharp
using FileEventStore;
using {ProjectName}.Api.Infrastructure;
using {ProjectName}.Core.Idempotency;
using {ProjectName}.Events;
using {ProjectName}.{BoundedContext1};
using {ProjectName}.{BoundedContext2};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// MediatR for event publishing and translations
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Program).Assembly,
    typeof({Aggregate1}).Assembly,
    typeof({Aggregate2}).Assembly
));

// FileEventStore
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "events");
builder.Services.AddFileEventStore(dataPath);

// Infrastructure
builder.Services.AddScoped<CorrelationContext>();
builder.Services.AddIdempotency();

// Event Publisher with correlation ID enrichment
builder.Services.AddScoped<MediatREventPublisher>();
builder.Services.AddScoped<IEventPublisher>(sp =>
{
    var inner = sp.GetRequiredService<MediatREventPublisher>();
    var correlationContext = sp.GetRequiredService<CorrelationContext>();
    var logger = sp.GetRequiredService<ILogger<CorrelationIdEventPublisher>>();
    return new CorrelationIdEventPublisher(inner, correlationContext, logger);
});

// Read model rebuilding on startup
builder.Services.AddHostedService<ReadModelInitializer>();

// Modules
builder.Services.Add{BoundedContext1}Module();
builder.Services.Add{BoundedContext2}Module();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseCorrelationId();
app.UseCors();

app.Map{BoundedContext1}Endpoints();
app.Map{BoundedContext2}Endpoints();

app.Run();

public partial class Program { }
```

### Shared Infrastructure Components

**DomainEventNotification wrapper:**
```csharp
// Events/DomainEventNotification.cs
using FileEventStore;
using MediatR;

namespace {ProjectName}.Events;

public record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IStoreableEvent;
```

**MediatR Event Publisher:**
```csharp
// Infrastructure/EventPublisher.cs
using FileEventStore;
using {ProjectName}.Events;
using MediatR;

namespace {ProjectName}.Api.Infrastructure;

public class MediatREventPublisher(IPublisher publisher) : IEventPublisher
{
    public async Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct = default)
    {
        foreach (var evt in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(evt.GetType());
            var notification = Activator.CreateInstance(notificationType, evt);
            await publisher.Publish(notification!, ct);
        }
    }
}
```

---

## Frontend (Angular)

The frontend mirrors the backend's bounded context structure using **Angular 19+ with zoneless change detection and signals**.

### App Configuration

```typescript
// src/app/app.config.ts
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),  // No zone.js!
    provideRouter(routes),
    provideHttpClient()
  ]
};
```

### Route Structure (Lazy Loading Per Context)

```typescript
// src/app/app.routes.ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '{context1}', pathMatch: 'full' },
  {
    path: '{context1}',
    loadChildren: () => import('./{context1}/{context1}.routes').then(m => m.{context1}Routes)
  },
  {
    path: '{context2}',
    loadChildren: () => import('./{context2}/{context2}.routes').then(m => m.{context2}Routes)
  }
];
```

### Bounded Context API Service

```typescript
// src/app/{context}/{context}.api.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { {Command}Command, {Command}Response, {Entity}Dto } from './{context}.types';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class {Context}Api {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Command with idempotency key
  {command}(command: {Command}Command, idempotencyKey?: string): Observable<{Command}Response> {
    const key = idempotencyKey || crypto.randomUUID();
    return this.http.post<{Command}Response>(
      `${this.baseUrl}/{resource}`,
      command,
      { headers: { 'Idempotency-Key': key } }
    );
  }

  // Query
  list{Entities}(): Observable<{Entity}Dto[]> {
    return this.http.get<{Entity}Dto[]>(`${this.baseUrl}/{resource}`);
  }
}
```

### Types (Match Backend DTOs)

```typescript
// src/app/{context}/{context}.types.ts

// Command
export interface {Command}Command {
  param1: string;
  param2: number;
}

// Response
export interface {Command}Response {
  entityId: string;
}

// DTO
export interface {Entity}Dto {
  entityId: string;
  name: string;
  status: string;
  createdAt: string;
  updatedAt?: string;
}
```

### Context Routes + Page Component

```typescript
// src/app/{context}/{context}.routes.ts
import { Component, ViewChild } from '@angular/core';
import { Routes } from '@angular/router';
import { {Create}Component } from './{create}.component';
import { {List}Component } from './{list}.component';

@Component({
  standalone: true,
  imports: [{Create}Component, {List}Component],
  template: `
    <div class="{context}-page">
      <h2>{Context}</h2>
      <app-{create} ({created})="{list}.load{Entities}()" />
      <app-{list} #{list} />
    </div>
  `,
  styles: [`.{context}-page { padding: 1rem; }`]
})
export class {Context}Page {
  @ViewChild({List}Component) {list}!: {List}Component;
}

export const {context}Routes: Routes = [
  { path: '', component: {Context}Page }
];
```

### Feature Component (Signals)

```typescript
// src/app/{context}/{create}.component.ts
import { Component, OnInit, signal, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { {Context}Api } from './{context}.api';

@Component({
  selector: 'app-{create}',
  standalone: true,
  imports: [FormsModule],
  template: `
    <h3>Create {Entity}</h3>
    <div class="form">
      <input type="text" [ngModel]="name()" (ngModelChange)="name.set($event)" placeholder="Name">
      <input type="number" [ngModel]="quantity()" (ngModelChange)="quantity.set($event)" min="1">
      <button (click)="submit()" [disabled]="!name()">
        Create
      </button>
    </div>
  `,
  styles: [`
    .form { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    input, button { padding: 0.5rem; }
    input[type="number"] { width: 80px; }
  `]
})
export class {Create}Component {
  // Signals for reactive state (zoneless)
  name = signal('');
  quantity = signal(1);

  // Output event
  {created} = output<void>();

  constructor(private api: {Context}Api) {}

  submit() {
    if (!this.name()) return;
    this.api.{command}({
      param1: this.name(),
      param2: this.quantity()
    }).subscribe(() => {
      this.name.set('');
      this.quantity.set(1);
      this.{created}.emit();
    });
  }
}
```

### List Component (Signals)

```typescript
// src/app/{context}/{list}.component.ts
import { Component, OnInit, signal } from '@angular/core';
import { {Context}Api } from './{context}.api';
import { {Entity}Dto } from './{context}.types';

@Component({
  selector: 'app-{list}',
  standalone: true,
  template: `
    <h3>{Entities}</h3>
    @if (items().length > 0) {
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          @for (item of items(); track item.entityId) {
            <tr [class]="item.status.toLowerCase()">
              <td>{{ item.entityId.slice(0, 8) }}</td>
              <td>{{ item.name }}</td>
              <td>{{ item.status }}</td>
              <td>
                @if (item.status === 'Active') {
                  <button (click)="doAction(item.entityId)">Action</button>
                } @else {
                  <span>✓</span>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    } @else {
      <p>No {entities} yet</p>
    }
  `,
  styles: [`
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.5rem; text-align: left; border-bottom: 1px solid #ddd; }
    button { padding: 0.25rem 0.75rem; cursor: pointer; }
  `]
})
export class {List}Component implements OnInit {
  items = signal<{Entity}Dto[]>([]);

  constructor(private api: {Context}Api) {}

  ngOnInit() {
    this.load{Entities}();
  }

  load{Entities}() {
    this.api.list{Entities}().subscribe(data => this.items.set(data));
  }

  doAction(entityId: string) {
    this.api.{action}(entityId).subscribe(() => this.load{Entities}());
  }
}
```

### Frontend Key Patterns

| Pattern | Implementation |
|---------|----------------|
| **State** | Signals (`signal<T>()`) - no zone.js |
| **Forms** | `FormsModule` + `ngModel` with signal updates |
| **Events** | `output<T>()` for parent communication |
| **Structure** | One folder per bounded context |
| **API** | Injectable service per context |
| **Types** | Mirror backend DTOs exactly |
| **Routing** | Lazy-load per context |
| **Components** | Standalone, inline templates |

### Checklist: Add New Frontend Feature

1. [ ] Add types to `{context}.types.ts` matching backend DTOs
2. [ ] Add API method to `{context}.api.ts`
3. [ ] Create `{feature}.component.ts` with signals
4. [ ] Import in page component and wire up events
5. [ ] If new context: add lazy route to `app.routes.ts`

---

## Testing Patterns

### Unit Tests for Aggregates

```csharp
public class {Aggregate}Tests
{
    private const string EntityId = "test-123";
    
    [Fact]
    public void {Action}_WithValidInput_Emits{Event}()
    {
        // Arrange
        var aggregate = new {Aggregate}Aggregate();
        aggregate.Initialize(EntityId, "Name");
        
        // Act
        aggregate.DoAction("param", 5);
        
        // Assert
        aggregate.UncommittedEvents.Should().HaveCount(2); // Init + Action
        var @event = aggregate.UncommittedEvents.Last()
            .Should().BeOfType<{Event}>().Subject;
        @event.EntityId.Should().Be(EntityId);
    }
    
    [Fact]
    public void {Action}_InWrongState_Throws()
    {
        // Arrange
        var aggregate = new {Aggregate}Aggregate();
        // Not initialized
        
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => aggregate.DoAction("param", 5));
        ex.Message.Should().Contain("Not initialized");
    }
    
    [Fact]
    public void StateTransitions_FollowExpectedFlow()
    {
        // Test the complete lifecycle
        var aggregate = new {Aggregate}Aggregate();
        aggregate.Status.Should().Be({Aggregate}Status.Initial);
        
        aggregate.Initialize(EntityId, "Name");
        aggregate.Status.Should().Be({Aggregate}Status.Ready);
        
        aggregate.Complete();
        aggregate.Status.Should().Be({Aggregate}Status.Completed);
    }
}
```

### Integration Tests

```csharp
public class {Context}IntegrationTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;
    
    public {Context}IntegrationTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }
    
    [Fact]
    public async Task Create{Entity}_ReturnsCreated()
    {
        // Arrange
        var command = new { Property = "Test", Amount = 10 };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/{entities}", command);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        result!.EntityId.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task IdempotentCreate_ReturnsSameResult()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new { Property = "Idempotent Test" };
        
        // Act - First request
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/{entities}")
        {
            Content = JsonContent.Create(command)
        };
        request1.Headers.Add("Idempotency-Key", idempotencyKey);
        var response1 = await _client.SendAsync(request1);
        var result1 = await response1.Content.ReadFromJsonAsync<CreateResponse>();
        
        // Act - Retry with same key
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/{entities}")
        {
            Content = JsonContent.Create(command)
        };
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        var response2 = await _client.SendAsync(request2);
        var result2 = await response2.Content.ReadFromJsonAsync<CreateResponse>();
        
        // Assert - Same EntityId returned
        result1!.EntityId.Should().Be(result2!.EntityId);
    }
}
```

### Cross-Context Translation Tests

```csharp
[Fact]
public async Task {Context1}Action_Triggers{Context2}Reaction()
{
    // Arrange - Set up preconditions in Context2
    await _client.PostAsJsonAsync("/api/{context2}/setup", new { ... });
    await Task.Delay(100); // Allow async processing
    
    // Act - Trigger action in Context1
    var response = await _client.PostAsJsonAsync("/api/{context1}/action", new { ... });
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    
    // Wait for translation
    await Task.Delay(500);
    
    // Assert - Verify Context2 was updated
    var result = await _client.GetFromJsonAsync<{Context2}Dto>("/api/{context2}/verify");
    result!.ExpectedProperty.Should().Be(expectedValue);
}
```

---

## Adding New Features Checklist

### New Command Feature
1. [ ] Create `Features/{FeatureName}.cs` with command, response, handler, endpoint
2. [ ] Add new events to `Events/{Context}/{Context}Events.cs` if needed
3. [ ] Update aggregate to handle new command in `Domain/{Aggregate}Aggregate.cs`
4. [ ] Register handler in `{Context}Module.cs` → `Add{Context}Module()`
5. [ ] Map endpoint in `{Context}Module.cs` → `Map{Context}Endpoints()`
6. [ ] Add unit tests for aggregate behavior
7. [ ] Add integration test for endpoint

### New Query Feature
1. [ ] Create `Features/{QueryName}.cs` with DTO, read model, projections, handler, endpoint
2. [ ] Register read model, projections, and handler in module
3. [ ] Map endpoint in module
4. [ ] Add read model to `ReadModelInitializer` if rebuilding on startup

### New Bounded Context
1. [ ] Create project `{ProjectName}.{NewContext}/`
2. [ ] Add events in `{ProjectName}.Events/{NewContext}/`
3. [ ] Create aggregate(s) in `Domain/`
4. [ ] Create feature files in `Features/`
5. [ ] Create `{NewContext}Module.cs`
6. [ ] Reference project from API
7. [ ] Register module in `Program.cs`
8. [ ] Add assembly to MediatR registration
9. [ ] Create test project

### Cross-Context Translation
1. [ ] Create `Features/{Reaction}On{Event}.cs` in the reacting context
2. [ ] Implement `INotificationHandler<DomainEventNotification<TEvent>>`
3. [ ] Register handler in module
4. [ ] Add integration test verifying the translation

---

## Best Practices

### DO
- ✅ One file per feature (vertical slice)
- ✅ Aggregates emit events, never mutate state directly
- ✅ Use records for commands, events, responses (immutable)
- ✅ Handle idempotency at the endpoint level
- ✅ Log with correlation IDs for tracing
- ✅ Test aggregates in isolation
- ✅ Use async translations via MediatR (not direct calls)

### DON'T
- ❌ Don't call other aggregates from within an aggregate
- ❌ Don't update read models directly from handlers (use events)
- ❌ Don't throw from translation handlers (log and continue)
- ❌ Don't share mutable state between contexts
- ❌ Don't put business logic in endpoints

---

## Quick Reference

| Concept | Location | Pattern |
|---------|----------|---------|
| **Command** | `{Context}/Features/{Name}.cs` | `record {Name}Command(...) : IIdempotentCommand` |
| **Event** | `Events/{Context}/{Context}Events.cs` | `record {Name}(props) : ICorrelatedEvent` |
| **Aggregate** | `{Context}/Domain/{Name}Aggregate.cs` | `class {Name}Aggregate : Aggregate` |
| **Read Model** | `{Context}/Features/{Query}.cs` | `class {Name}ReadModel` + projections |
| **Translation** | `{Context}/Features/{Reaction}On{Event}.cs` | `INotificationHandler<DomainEventNotification<T>>` |
| **Module** | `{Context}/{Context}Module.cs` | `Add{X}Module()` + `Map{X}Endpoints()` |
| **Endpoint** | `{Context}/Features/{Name}.cs` | `static class {Name}Endpoint { Map(app) }` |

---

## Tech Stack Reference

### Backend
- **.NET 8+** / Minimal APIs
- **FileEventStore** - File-based event sourcing (swap for Marten/EventStoreDB in production)
- **MediatR** - Event publishing and cross-context translations
- **FluentAssertions** - Test assertions
- **xUnit** - Test framework

### Frontend
- **Angular 19+** - Zoneless with signals
- **Standalone Components** - No NgModules
- **Lazy Loading** - Per bounded context
- **Signals** - Reactive state management (`signal<T>()`, `computed()`)
- **Output** - Event emission to parent (`output<T>()`)
