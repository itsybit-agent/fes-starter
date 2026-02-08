# Claude Code Skills

This directory contains Claude Code skills and instructions for scaffolding features in any CQRS + Event Sourcing project.

## Quick Start

### Using the Scaffold Skill

When you want to generate a complete feature, use:

```
/scaffold-feature {Context} {Feature} {Description}
```

**Examples:**
```
# Auto-detects project name from .sln/.csproj
/scaffold-feature Orders PlaceOrder "Create a new order for a product"

# Or specify project name explicitly
/scaffold-feature Payments ProcessRefund "Process refund" --project-name MyCompany
/scaffold-feature Inventory AdjustStock "Adjust stock" --project-name Acme
```

### What It Generates

The skill creates:

**Backend (.NET)**
- Domain event(s) in `src/{ProjectName}.Events/`
- Aggregate method in `src/{ProjectName}.{Context}/`
- Complete feature file with command, handler, read model, endpoint
- Module registration updates
- Integration tests

**Frontend (Angular)**
- TypeScript types/DTOs
- API service
- Component with error handling
- Route registration

**Automation**
- Builds and verifies everything works
- Runs tests
- Shows summary

## Files in This Directory

| File | Purpose |
|------|---------|
| `skills/scaffold-feature.md` | Skill definition and metadata (generic) |
| `scaffold-implementation.md` | Technical details of what gets generated |
| `scaffold-prompt.md` | Claude prompt instructions (used internally) |
| `skill-example.md` | Real example: RefundOrder feature |
| `SKILL_ARCHITECTURE.md` | Flow diagram & integration patterns |
| `README.md` | This file |

## How to Use

### From Claude Code CLI

If you have Claude Code set up locally:

```bash
claude code /scaffold-feature Orders PlaceOrder "Create a new order"
```

### From Claude Web Interface

When working with any CQRS + Event Sourcing project:

```
/scaffold-feature Payments ProcessRefund "Process refund for an order"
/scaffold-feature Inventory AdjustStock "Adjust stock" --project-name MyApp
```

### From Any Machine

The skill is stored in the repository, so:

1. Clone the project
2. Ask Claude to scaffold a feature
3. It automatically detects your project name and applies the skill

## Examples

### Example 1: Create Order Feature (Auto-Detect)
```
/scaffold-feature Orders PlaceOrder "Create a new order with product and quantity"
```

Detects project name from `.sln` or `.csproj` (e.g., `MyApp`) and generates:
- `src/MyApp.Events/Orders/PlaceOrderEvents.cs`
- `src/MyApp.Orders/Features/PlaceOrder.cs`
- `src/MyApp.Web/src/app/orders/place-order.api.ts`
- `src/MyApp.Web/src/app/orders/place-order.component.ts`
- `tests/MyApp.Api.Tests/PlaceOrderTests.cs`

### Example 2: Create Payment Feature (Explicit Name)
```
/scaffold-feature Payments ProcessPayment "Process payment for an order" --project-name Acme
```

Explicitly uses `Acme` as project name and generates:
- `src/Acme.Events/Payments/ProcessPaymentEvents.cs`
- `src/Acme.Payments/Features/ProcessPayment.cs`
- Full Angular component with error handling
- Tests and module registration

### Example 3: Create Shipping Feature
```
/scaffold-feature Shipping ShipOrder "Ship an order to customer" --project-name Logistics
```

Generates complete feature in `Logistics` project namespace with all integration

## For Different Machines

### Same Team
```bash
# Clone your project (any CQRS + Event Sourcing project)
git clone https://github.com/myteam/MyProject.git
cd MyProject

# Use the skill immediately (auto-detects project name)
/scaffold-feature Orders PlaceOrder "..."
```

### Different Project
```bash
# Clone another project with different naming
git clone https://github.com/otherteam/DifferentProject.git
cd DifferentProject

# Skill works the same way - auto-detects "DifferentProject"
/scaffold-feature Payments ProcessRefund "..."
```

### Custom Project Name
```bash
# If auto-detection fails or you want to override:
/scaffold-feature Inventory AdjustStock "..." --project-name MyCompany
```

### Remote Team
Everyone who clones a project with `.claude/` directory gets access to:
- All skill definitions (generic, no hardcoded names)
- SCAFFOLDING.md documentation
- Code examples
- Full instructions

## Understanding the Generated Code

All generated features follow CQRS + Event Sourcing patterns:

### Backend Pattern
```
Event → Aggregate Logic → Command → Handler → Endpoint
         ↓
      Event Stream
         ↓
    Read Model (Singleton)
         ↓
    Angular Service → Component
```

### Key Properties

✅ **Idempotent** - All commands safe to retry
✅ **Event-sourced** - Full audit trail
✅ **Error-handled** - Toast notifications on frontend
✅ **Tested** - Integration tests generated
✅ **Type-safe** - Full TypeScript + C# typing

## Customizing Generated Code

After scaffolding, you typically:

1. **Add validation rules** to aggregate methods
2. **Enhance event handling** for cross-context reactions
3. **Style the component** with custom CSS
4. **Add UI features** (loading state, confirmation dialogs)
5. **Extend tests** with edge cases

## Troubleshooting

### "Skill not found"
Ensure you're in a project with `.claude/skills/` directory:
```bash
ls .claude/skills/scaffold-feature.md
```

### "Generated code won't build"
1. Check imports are correct
2. Verify context name matches existing projects
3. Run `dotnet clean && dotnet build`

### "Components not rendering"
1. Check routes are registered in `{context}.routes.ts`
2. Verify `ToastComponent` is in root app
3. Check `ToastService` is imported

## Creating New Skills

To extend with new skills:

1. Create `skills/my-new-skill.md` with description
2. Update `scaffold-prompt.md` if adding to scaffolding
3. Add example in `skill-example.md`
4. Document in this README

## Next Steps

- **Read** `SCAFFOLDING.md` for deep architecture knowledge
- **Try** scaffolding your first feature
- **Review** generated code and customize
- **Build** and test locally
- **Commit** to your branch

## References

- **Architecture**: See `CLAUDE.md` in project root
- **Detailed Patterns**: See `SCAFFOLDING.md` in project root
- **Skill System**: See `.claude/` directory
