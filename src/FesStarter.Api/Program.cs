using FileEventStore;
using FesStarter.Api.Features.Orders;
using FesStarter.Api.Features.Orders.PlaceOrder;
using FesStarter.Api.Features.Orders.ShipOrder;
using FesStarter.Api.Features.Orders.ListOrders;
using FesStarter.Api.Features.Inventory;
using FesStarter.Api.Features.Inventory.InitializeStock;
using FesStarter.Api.Features.Inventory.GetStock;
using FesStarter.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// MediatR for event publishing and translations
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// FileEventStore
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "events");
builder.Services.AddFileEventStore(dataPath);

// Event publisher
builder.Services.AddScoped<IEventPublisher, MediatREventPublisher>();

// Read models (singleton - in production, use a proper database)
builder.Services.AddSingleton<OrderReadModel>();
builder.Services.AddSingleton<StockReadModel>();
builder.Services.AddHostedService<ReadModelInitializer>();

// Order handlers
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddScoped<ShipOrderHandler>();
builder.Services.AddScoped<ListOrdersHandler>();

// Inventory handlers
builder.Services.AddScoped<InitializeStockHandler>();
builder.Services.AddScoped<GetStockHandler>();

// CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Aspire health endpoints
app.MapDefaultEndpoints();

// Map endpoints (vertical slices)
// Orders
app.MapPlaceOrder();
app.MapShipOrder();
app.MapListOrders();

// Inventory
app.MapInitializeStock();
app.MapGetStock();

app.Run();
