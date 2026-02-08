# Claude Code Skill: Scaffold Feature

## System Instructions

When a user invokes `/scaffold-feature {context} {feature} {description} [--project-name {ProjectName}]`, follow these steps:

### Step 1: Parse Input
Extract:
- **Context**: Bounded context name (e.g., Orders, Inventory, Payments)
- **Feature**: Feature name in PascalCase (e.g., PlaceOrder, AdjustStock)
- **Description**: One-line feature description
- **ProjectName**: (Optional) Root namespace. Auto-detect from .sln/.csproj if not provided

Example mappings:
```
User: /scaffold-feature Orders PlaceOrder "..."
Project Detection: Look for {Name}.sln or {Name}.Orders.csproj
Result: ProjectName = "MyCompany" or "Acme" or auto-detected

User: /scaffold-feature Orders PlaceOrder "..." --project-name Acme
Result: ProjectName = "Acme"
```

### Step 2: Read Reference Files
1. Read `SCAFFOLDING.md` for patterns
2. Read `.claude/scaffold-implementation.md` for templates
3. Check existing context in `FesStarter.{Context}/` for patterns

### Step 3: Generate Events
**File:** `src/{ProjectName}.Events/{Context}/{Feature}Events.cs`

```csharp
namespace {ProjectName}.Events.{Context};

public record {Feature}Started(
    string Id,
    // Add relevant fields based on description
    DateTime Timestamp = default
) : DomainEvent
{
    public {Feature}Started() : this("") { }
}

public record {Feature}Completed(
    string Id,
    DateTime CompletedAt = default
) : DomainEvent
{
    public {Feature}Completed() : this("") { }
}
```

### Step 4: Generate Aggregate Logic
**File:** `src/{ProjectName}.{Context}/{Aggregate}.cs` (append to existing)

Add method to aggregate:
```csharp
public void {Feature}(/* parameters */)
{
    // Add validation
    if (/* condition */) throw new ArgumentException("...");

    // Apply event
    ApplyEvent(new {Feature}Started(Id, /* args */));
}

private void ApplyEvent({Feature}Started evt)
{
    // Update aggregate state from event
    AddUncommittedEvent(evt);
}
```

### Step 5: Generate Feature File
**File:** `src/{ProjectName}.{Context}/Features/{Feature}.cs`

Include all of:
1. **Command** (with IIdempotentCommand)
2. **Response** DTO
3. **Handler** (with error handling)
4. **Endpoint** (with idempotency)
5. **Read Model** (if applicable)

Key patterns:
- All commands implement `IIdempotentCommand`
- All endpoints use `IIdempotencyService.GetOrExecuteAsync`
- Include cancellation token: `ct: request.HttpContext.RequestAborted`
- Handle null responses with 500 error
- Add proper XML documentation

### Step 6: Generate Frontend Files

**A. Types** - `src/FesStarter.Web/src/app/{context}/{feature}.types.ts`
```typescript
export interface {Feature}Dto { ... }
export interface {Feature}Command { ... }
export interface {Feature}Response { ... }
```

**B. API Service** - `src/FesStarter.Web/src/app/{context}/{feature}.api.ts`
```typescript
@Injectable({ providedIn: 'root' })
export class {Context}Api {
  {feature}(command: {Feature}Command): Observable<{Feature}Response> {
    const key = crypto.randomUUID();
    return this.http.post<{Feature}Response>(
      `${this.baseUrl}/...`,
      command,
      { headers: { 'Idempotency-Key': key } }
    );
  }
}
```

**C. Component** - `src/FesStarter.Web/src/app/{context}/{feature}.component.ts`
```typescript
export class {Feature}Component {
  constructor(
    private api: {Context}Api,
    private toast: ToastService
  ) {}

  {feature}() {
    this.api.{feature}({ ... }).subscribe({
      next: () => this.toast.success('{Feature} successful'),
      error: () => this.toast.error('Failed to {feature}')
    });
  }
}
```

### Step 7: Update Module

**File:** `src/{ProjectName}.{Context}/{Context}Module.cs`

Add to `Add{Context}Module()`:
```csharp
services.AddScoped<{Feature}Handler>();
// Add read model if needed
services.AddSingleton<{Feature}ReadModel>();
```

Add to `Map{Context}Endpoints()`:
```csharp
{Feature}Endpoint.Map(app);
```

### Step 8: Update Routes (Frontend)

**File:** `src/FesStarter.Web/src/app/{context}/{context}.routes.ts`

Add route:
```typescript
{
  path: '{feature-kebab}',
  component: {Feature}Component
}
```

### Step 9: Generate Tests

**File:** `tests/{ProjectName}.Api.Tests/{Feature}Tests.cs`

```csharp
[Fact]
public async Task {Feature}_WithValidCommand_ReturnsSuccess()
{
    // Arrange
    var command = new {Feature}Command(...);

    // Act
    var response = await handler.HandleAsync(command);

    // Assert
    Assert.NotNull(response);
}

[Fact]
public async Task {Feature}_WithInvalidData_ThrowsException()
{
    // Arrange
    var command = new {Feature}Command(/* invalid */);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        handler.HandleAsync(command));
}
```

### Step 10: Verify & Build

1. **Build Backend**
   ```bash
   dotnet build src/{ProjectName}.Api/
   ```

2. **Build Frontend**
   ```bash
   cd src/FesStarter.Web && npm run build
   ```

3. **Run Tests**
   ```bash
   dotnet test tests/{ProjectName}.Api.Tests/ -v minimal
   ```

### Step 11: Summary Output

Provide user with:
```
✅ Scaffolding Complete: {Feature}

Generated Files:
  ✓ src/{ProjectName}.Events/{Context}/{Feature}Events.cs
  ✓ src/{ProjectName}.{Context}/Features/{Feature}.cs
  ✓ src/{ProjectName}.Web/src/app/{context}/{feature}.api.ts
  ✓ src/{ProjectName}.Web/src/app/{context}/{feature}.component.ts
  ✓ tests/{ProjectName}.Api.Tests/{Feature}Tests.cs

Updated:
  ✓ src/{ProjectName}.{Context}/{Context}Module.cs
  ✓ src/{ProjectName}.Web/src/app/{context}/{context}.routes.ts

Build Status: ✅ Success

Next Steps:
1. Review generated code
2. Add domain validations to aggregate
3. Customize frontend component UI
4. Wire event projections if needed
5. Commit: git commit -m "feat: Add {Feature} feature"
```

## Error Handling

If any step fails:
1. Report which step failed
2. Show the error message
3. Suggest fixes
4. Offer to retry or manually create files

## Customization

When generating, consider:
- Existing patterns in the context
- Domain-specific validations
- Related aggregates that might need updates
- Cross-context event communication needs
- Frontend workflow (component nesting, navigation)

## Reference

- CLAUDE.md - Architecture overview
- SCAFFOLDING.md - Detailed patterns and examples
- src/FesStarter.Orders/ - Real example (command pattern)
- src/FesStarter.Inventory/ - Real example (query pattern)
