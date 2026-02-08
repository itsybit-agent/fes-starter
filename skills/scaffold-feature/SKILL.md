# scaffold-feature

Add a new feature to an existing fes-starter project.

## Usage

```
Add a {FeatureName} feature to the {Context} module: {description}
```

## How It Works

**Don't generate from scratch. Copy existing patterns.**

## Steps

### 1. Find the template

Look for an existing feature to copy:
```
src/{ProjectName}.{Context}/Features/*.cs
```

If the context is new, copy from Orders or Inventory module.

### 2. Copy and rename

For a new feature `CancelOrder` in `Orders`:

| Copy From | Create |
|-----------|--------|
| `PlaceOrder.cs` | `CancelOrder.cs` |
| `OrderPlaced.cs` (in Events) | `OrderCancelled.cs` |
| `place-order.component.ts` | `cancel-order.component.ts` |

### 3. Find and replace

In the copied files, replace:
- `PlaceOrder` → `CancelOrder`
- `OrderPlaced` → `OrderCancelled`
- `place-order` → `cancel-order`
- Update the description/properties for the new feature

### 4. Wire it up

Add to module's `MapEndpoints()`:
```csharp
group.MapPost("/cancel", CancelOrder.Endpoint);
```

Add to Angular routes:
```typescript
{ path: 'cancel', component: CancelOrderComponent }
```

### 5. Customize

Now adjust the copied code:
- Change command properties
- Change event properties  
- Update aggregate method
- Update component form/display
- Update tests

## That's it

The existing code IS the template. Copy it, rename it, customize it.

## Reference Files

Read these to understand the patterns:
- `SCAFFOLDING.md` - detailed patterns
- `src/*/Features/*.cs` - backend examples
- `src/*/Web/src/app/*` - frontend examples
