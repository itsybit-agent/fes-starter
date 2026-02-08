# FesStarter Claude Code Skills

This directory contains Claude Code skills and instructions for scaffolding FesStarter features.

## Quick Start

### Using the Scaffold Skill

When you want to generate a complete feature, use:

```
/scaffold-fes-feature {Context} {Feature} {Description}
```

**Example:**
```
/scaffold-fes-feature Orders PlaceOrder "Create a new order for a product"
```

### What It Generates

The skill creates:

**Backend (.NET)**
- Domain event(s) in `FesStarter.Events/`
- Aggregate method in `FesStarter.{Context}/`
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
| `skills/scaffold-fes-feature.md` | Skill definition and metadata |
| `scaffold-implementation.md` | Technical details of what gets generated |
| `scaffold-prompt.md` | Claude prompt instructions (used internally) |
| `skill-example.md` | Real example: RefundOrder feature |
| `README.md` | This file |

## How to Use

### From Claude Code CLI

If you have Claude Code set up locally:

```bash
claude code /scaffold-fes-feature Orders PlaceOrder "Create a new order"
```

### From Claude Web Interface

When working with a FesStarter project:

```
/scaffold-fes-feature Payments ProcessRefund "Process refund for an order"
```

### From Any Machine

The skill is stored in the repository, so:

1. Clone FesStarter
2. Ask Claude to scaffold a feature
3. It automatically references the skill definition

## Examples

### Example 1: Create Order Feature
```
/scaffold-fes-feature Orders PlaceOrder "Create a new order with product and quantity"
```

Generates:
- `OrderPlaced` event
- `PlaceOrder` command + handler + endpoint
- `PlaceOrderComponent` with form
- Integration tests
- All wiring updated

### Example 2: Create Payment Feature
```
/scaffold-fes-feature Payments ProcessPayment "Process payment for an order"
```

Generates:
- `PaymentProcessed` event
- `ProcessPayment` command + handler
- `PaymentComponent` with form
- Tests for success and failure cases
- Module registration

### Example 3: Create Shipping Feature
```
/scaffold-fes-feature Shipping ShipOrder "Ship an order to customer"
```

Generates:
- `OrderShipped` event
- `ShipOrder` command + handler
- `ShipOrderComponent`
- Cross-context event handling setup

## Understanding the Generated Code

All generated features follow FesStarter patterns:

### Backend Pattern
```
Event → Aggregate Logic → Command → Handler → Endpoint
         ↓
      Event Stream (FileEventStore)
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
Ensure you're in a FesStarter project:
```bash
ls .claude/skills/scaffold-fes-feature.md
```

### "Generated code won't build"
1. Check imports are correct
2. Verify context name matches existing projects
3. Run `dotnet clean && dotnet build`

### "Components not rendering"
1. Check routes are registered in `{context}.routes.ts`
2. Verify `ToastComponent` is in root app
3. Check `ToastService` is imported

## For Different Machines

### Same Team
```bash
# Clone FesStarter
git clone https://github.com/itsybit-agent/fes-starter.git MyProject

# Use the skill immediately
/scaffold-fes-feature Orders PlaceOrder "..."
```

### Different Project (Starting Fresh)
```bash
# Reference the skill documentation
# https://raw.githubusercontent.com/itsybit-agent/fes-starter/master/.claude/skills/scaffold-fes-feature.md

# Or copy SCAFFOLDING.md and reference patterns manually
```

### Remote Team
Everyone who clones FesStarter gets access to:
- All skill definitions
- SCAFFOLDING.md documentation
- Real code examples (Orders, Inventory)
- This README

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
- **Real Examples**: See `src/FesStarter.Orders/` and `src/FesStarter.Inventory/`
