# scaffold-module

Add a new bounded context/module to this project.

## Usage

```
/scaffold-module {ModuleName} "{description}"
```

## Examples

```
/scaffold-module Payments "Payment processing and refunds"
/scaffold-module Shipping "Order shipping and tracking"
```

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

| Find | Replace |
|------|---------|
| `Orders` | `{ModuleName}` |
| `orders` | `{moduleName}` (lowercase) |
| `Order` | `{Entity}` (singular, e.g., `Payment`) |
| `order` | `{entity}` (lowercase singular) |

Update:
- `{ModuleName}Module.cs`
- `Domain/{Entity}Aggregate.cs`
- `Features/*.cs`
- All namespaces

### 4. Create events folder

```bash
mkdir -p src/{ProjectName}.Events/{ModuleName}
```

### 5. Add to solution

```bash
dotnet sln add src/{ProjectName}.{ModuleName}/{ProjectName}.{ModuleName}.csproj
```

### 6. Reference from API

Edit `src/{ProjectName}.Api/{ProjectName}.Api.csproj`:
```xml
<ProjectReference Include="..\{ProjectName}.{ModuleName}\{ProjectName}.{ModuleName}.csproj" />
```

### 7. Register in Program.cs

```csharp
builder.Services.Add{ModuleName}Module();
app.Map{ModuleName}Endpoints();
```

### 8. Clean up

Delete copied example features, keep one as template.

## Reference

Copy from `src/{ProjectName}.Orders/`
