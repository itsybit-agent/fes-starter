# Scaffold Skill Architecture

## How the Skill Works

```
┌─────────────────────────────────────────────────────────────────┐
│  User Input (Any Machine)                                       │
│  /scaffold-fes-feature Orders PlaceOrder "Create new order"     │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Skill Resolution                                               │
│  - Read .claude/skills/scaffold-fes-feature.md                  │
│  - Load scaffold-prompt.md instructions                         │
│  - Reference SCAFFOLDING.md patterns                            │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Code Generation (10 Steps)                                     │
│                                                                 │
│  1. Parse context: "Orders"                                     │
│  2. Parse feature: "PlaceOrder"                                 │
│  3. Read existing Orders context structure                      │
│  4. Generate Events (PlaceOrderStarted, PlaceOrderCompleted)    │
│  5. Add Aggregate method (Place)                                │
│  6. Create Feature file (Command+Handler+Endpoint+ReadModel)    │
│  7. Create Types (.types.ts)                                    │
│  8. Create API Service (.api.ts)                                │
│  9. Create Component (.component.ts)                            │
│  10. Update Module registration & Routes                        │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Generated Files (Organized by Layer)                           │
│                                                                 │
│  🟧 Events Layer          🟦 Command Layer                      │
│  ├─ OrderPlaced           ├─ PlaceOrderCommand                  │
│  └─ OrderCompleted        └─ PlaceOrderResponse                 │
│                                                                 │
│  📗 Domain Layer          ⚙️ Handler Layer                      │
│  ├─ OrderAggregate        ├─ PlaceOrderHandler                  │
│  │  └─ Place()            └─ HandleAsync()                      │
│  └─ OrderReadModel                                              │
│                                                                 │
│  🔌 API Layer             🎨 Frontend Layer                     │
│  ├─ PlaceOrderEndpoint    ├─ PlaceOrderComponent                │
│  └─ /api/orders [POST]    ├─ OrdersApi.placeOrder()             │
│                           └─ placeOrder.types.ts                │
│                                                                 │
│  ✅ Tests Layer           🔧 Config Layer                       │
│  ├─ PlaceOrderTests.cs    ├─ OrdersModule.cs (updated)          │
│  └─ Idempotency tests     └─ orders.routes.ts (updated)         │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Verification & Build                                           │
│                                                                 │
│  ✓ Build .NET: dotnet build src/FesStarter.Api/                │
│  ✓ Build Angular: npm run build                                 │
│  ✓ Run Tests: dotnet test tests/                                │
│  ✓ Summary: Report all generated files                          │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  Output Summary                                                 │
│                                                                 │
│  ✅ Scaffolding Complete: PlaceOrder                            │
│  Generated 11 files, Updated 2 files                            │
│  Build Status: ✅ Success                                        │
│  Tests: 18/18 passing                                           │
│                                                                 │
│  Ready to: Review → Customize → Test → Commit                  │
└─────────────────────────────────────────────────────────────────┘
```

## File Organization

```
.claude/                                    (Skill Infrastructure)
├── skills/
│   └── scaffold-fes-feature.md            (Skill Definition)
├── README.md                              (Usage Guide)
├── scaffold-prompt.md                     (Claude Prompt)
├── scaffold-implementation.md             (Technical Details)
└── SKILL_ARCHITECTURE.md                  (This File)

SCAFFOLDING.md                             (Reference Patterns)
CLAUDE.md                                  (Architecture)

src/FesStarter.Events/                     (Generated Events)
src/FesStarter.{Context}/
├── {Aggregate}.cs                         (Aggregate Logic)
└── Features/
    └── {Feature}.cs                       (Generated Feature)

src/FesStarter.Web/src/app/{context}/      (Generated Frontend)
├── {feature}.types.ts
├── {context}.api.ts
├── {feature}.component.ts
└── {context}.routes.ts

tests/FesStarter.Api.Tests/
└── {Feature}Tests.cs                      (Generated Tests)
```

## Skill Portability

### Same Machine, Different Project

```bash
# Clone FesStarter
git clone https://github.com/itsybit-agent/fes-starter.git ProjectA
cd ProjectA

# Use skill immediately
/scaffold-fes-feature Orders PlaceOrder "..."
```

### Different Machine, Same Team

```bash
# Team member clones project
git clone https://github.com/myteam/ProjectA.git
cd ProjectA

# Skill available since it's in .claude/
/scaffold-fes-feature Payments ProcessRefund "..."
```

### Different Project, Different Tech Stack

```bash
# Reference skill documentation
https://raw.githubusercontent.com/itsybit-agent/fes-starter/master/.claude/skills/scaffold-fes-feature.md

# Reference SCAFFOLDING.md for patterns
https://raw.githubusercontent.com/itsybit-agent/fes-starter/master/SCAFFOLDING.md

# Implement manually in your tech stack
```

## Customization Points

After scaffolding, you customize:

```
Generated Code
    │
    ├─ Events: Add more event types
    ├─ Aggregate: Add validation rules
    ├─ Handler: Add external service calls
    ├─ ReadModel: Add more query methods
    ├─ Endpoint: Add middleware, auth
    ├─ Component: Add UI features
    └─ Tests: Add edge cases
```

## Quality Assurance

Each generated feature includes:

```
✅ Type Safety
   - C# nullable reference types
   - TypeScript strict mode
   - Full typing on APIs

✅ Error Handling
   - Try-catch in handlers
   - Toast notifications on frontend
   - Validation in aggregates

✅ Idempotency
   - IIdempotentCommand interface
   - IIdempotencyService integration
   - Cancellation token support

✅ Testing
   - Happy path test
   - Error case test
   - Idempotency test

✅ Documentation
   - XML comments in C#
   - JSDoc in TypeScript
   - Module comments
```

## Integration with Existing Code

```
Before Scaffold              After Scaffold
─────────────────           ───────────────

Orders/                      Orders/
├─ OrderAggregate.cs    →   ├─ OrderAggregate.cs (enhanced)
└─ OrdersModule.cs      →   ├─ Features/
   └─ Map/Add methods        │  ├─ PlaceOrder.cs (new)
                             │  ├─ ShipOrder.cs (existing)
                             │  └─ ListOrders.cs (existing)
                             └─ OrdersModule.cs (updated)

                        Events/Orders/ (new files)
                        ├─ OrderPlacedEvents.cs
                        └─ OrderShippedEvents.cs

                        Frontend orders/ (new/updated)
                        ├─ place-order.component.ts
                        ├─ orders.api.ts (updated)
                        └─ orders.routes.ts (updated)
```

## Next Generation

Potential extensions to the skill:

- **Saga Scaffolding**: Generate multi-step workflows
- **Policy Scaffolding**: Generate cross-context event handlers
- **Report Scaffolding**: Generate read-only reporting features
- **External API Integration**: Generate service clients
- **GraphQL Support**: Generate GraphQL types and resolvers
- **gRPC Support**: Generate service definitions
