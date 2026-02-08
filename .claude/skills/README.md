# Scaffold Skills

These skills help Claude scaffold new code by copying existing patterns.

## Available Skills

| Skill | Usage | What it creates |
|-------|-------|-----------------|
| **scaffold-module** | `/scaffold-module Payments "description"` | New bounded context project |
| **scaffold-command** | `/scaffold-command Orders CancelOrder "description"` | Write operation (command + event + endpoint) |
| **scaffold-query** | `/scaffold-query Orders GetOrderDetails "description"` | Read operation (query + read model + endpoint) |
| **scaffold-automation** | `/scaffold-automation Inventory ReserveStockOnOrderPlaced "description"` | Cross-context event handler |

## Quick Examples

### Add a new module (bounded context)

```
/scaffold-module Payments "Payment processing and refunds"
```

Creates:
- `src/{Project}.Payments/` — new module project
- `src/{Project}.Events/Payments/` — events folder
- Wired into solution and Program.cs

### Add a command (write operation)

```
/scaffold-command Payments ProcessPayment "Process a payment for an order"
```

Creates:
- `PaymentProcessed` event
- `ProcessPaymentCommand` + handler
- POST endpoint at `/api/payments/process`
- Aggregate method

### Add a query (read operation)

```
/scaffold-query Payments ListPayments "List payments with optional filtering"
```

Creates:
- `ListPaymentsQuery` + handler
- `PaymentDto` + read model
- GET endpoint at `/api/payments`
- Projection for read model updates

### Add an automation (event handler)

```
/scaffold-automation Payments ChargeCardOnOrderConfirmed "Charge payment when order confirmed"
```

Creates:
- Event handler reacting to `OrderConfirmed`
- May emit `CardCharged` event
- No endpoint (internal)

## How It Works

All skills use the **copy-based approach**:

1. Find existing code that does something similar
2. Copy the file(s)
3. Find/replace names
4. Customize for the specific use case

This is more reliable than generating from scratch — the existing code IS the template.

## Tips

- Start with `/scaffold-module` for new bounded contexts
- Use `/scaffold-command` for actions that change state
- Use `/scaffold-query` for reading data
- Use `/scaffold-automation` for cross-module reactions

## Reference

See the individual skill files for detailed steps:
- [scaffold-module.md](./scaffold-module.md)
- [scaffold-command.md](./scaffold-command.md)
- [scaffold-query.md](./scaffold-query.md)
- [scaffold-automation.md](./scaffold-automation.md)
