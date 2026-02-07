using FileEventStore;
using FesStarter.Api.Features.Orders;
using FesStarter.Api.Features.Inventory;
using FesStarter.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// MediatR for event publishing and translations
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// FileEventStore
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "events");
builder.Services.AddFileEventStore(dataPath);

// Infrastructure
builder.Services.AddScoped<IEventPublisher, MediatREventPublisher>();
builder.Services.AddSingleton<OrderReadModel>();
builder.Services.AddSingleton<StockReadModel>();
builder.Services.AddHostedService<ReadModelInitializer>();

// Handlers
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddScoped<ShipOrderHandler>();
builder.Services.AddScoped<ListOrdersHandler>();
builder.Services.AddScoped<InitializeStockHandler>();
builder.Services.AddScoped<GetStockHandler>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseCors();
app.MapDefaultEndpoints();

// Orders
PlaceOrderEndpoint.Map(app);
ShipOrderEndpoint.Map(app);
ListOrdersEndpoint.Map(app);

// Inventory
InitializeStockEndpoint.Map(app);
GetStockEndpoint.Map(app);

app.Run();
