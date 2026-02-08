# scaffold-module

Add a new bounded context/module to this project.

## Usage

```
/scaffold-module {ModuleName} "{description}"
```

or

```
Add a {ModuleName} module: {description}
```

## Examples

```
/scaffold-module Payments "Payment processing and refunds"
/scaffold-module Shipping "Order shipping and tracking"
```

## How It Works

**Copy an existing module, rename everything.**

## Steps

### 0. Detect project name

Find `*.slnx` or `*.sln` — filename is the ProjectName (e.g., `Acme`).

### 1. Copy the module project

```bash
cp -r src/{ProjectName}.Orders src/{ProjectName}.{ModuleName}
```

### 2. Rename the .csproj

```bash
mv src/{ProjectName}.{ModuleName}/{ProjectName}.Orders.csproj \
   src/{ProjectName}.{ModuleName}/{ProjectName}.{ModuleName}.csproj
```

### 3. Find and replace in all files

In `src/{ProjectName}.{ModuleName}/`:

| Find | Replace |
|------|---------|
| `Orders` | `{ModuleName}` |
| `orders` | `{moduleName}` (lowercase) |
| `Order` | `{Entity}` (singular, e.g., `Payment`) |
| `order` | `{entity}` (lowercase singular) |

Files to update:
- `{ModuleName}Module.cs`
- `Domain/{Entity}Aggregate.cs`
- `Features/*.cs`
- All `namespace` and `using` statements

### 4. Create events

```bash
mkdir -p src/{ProjectName}.Events/{ModuleName}
```

Create initial event (copy from Orders):
```csharp
// src/{ProjectName}.Events/{ModuleName}/{Entity}Events.cs
namespace {ProjectName}.Events.{ModuleName};

public record {Entity}Created(
    Guid {Entity}Id,
    // ... properties
) : IDomainEvent;
```

### 5. Add to solution

```bash
dotnet sln add src/{ProjectName}.{ModuleName}
```

### 6. Reference from API

Edit `src/{ProjectName}.Api/{ProjectName}.Api.csproj`:
```xml
<ProjectReference Include="..\{ProjectName}.{ModuleName}\{ProjectName}.{ModuleName}.csproj" />
```

### 7. Register in Program.cs

Edit `src/{ProjectName}.Api/Program.cs`:
```csharp
// Add with other module registrations
builder.Services.Add{ModuleName}Module(builder.Configuration);

// Add with other endpoint mappings
app.Map{ModuleName}Endpoints();
```

### 8. Clean up example features

Delete or rename the copied features:
- Remove `PlaceOrder.cs`, `ShipOrder.cs`, etc.
- Keep one as a template, rename to `Create{Entity}.cs`

### 9. Add frontend route (optional)

Create `src/{ProjectName}.Web/src/app/{modulename}/` folder with:
- `{modulename}.routes.ts`
- Basic component

Add to `app.routes.ts`:
```typescript
{ path: '{modulename}', loadChildren: () => import('./{modulename}/{modulename}.routes') }
```

## Summary output

After completion, report:
- ✅ Created `src/{ProjectName}.{ModuleName}/`
- ✅ Created `src/{ProjectName}.Events/{ModuleName}/`
- ✅ Added to solution
- ✅ Registered in API
- 📝 Next: Use `/scaffold-feature` to add features

## Reference

Copy structure from `src/{ProjectName}.Orders/` — that's your template.
