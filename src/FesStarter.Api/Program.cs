using FileEventStore;
using FesStarter.Api.Infrastructure;
using FesStarter.Events;
using FesStarter.Orders;
using FesStarter.Inventory;

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
builder.Services.AddHostedService<ReadModelInitializer>();

// Modules
builder.Services.AddOrdersModule();
builder.Services.AddInventoryModule();

// Read model projections (eventually consistent event-driven updates)
builder.Services.AddSingleton<FesStarter.Api.Features.Projections.OrderReadModelProjections>();
builder.Services.AddSingleton<FesStarter.Api.Features.Projections.StockReadModelProjections>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseCors();
app.MapDefaultEndpoints();

// Module endpoints
app.MapOrderEndpoints();
app.MapInventoryEndpoints();

app.Run();
