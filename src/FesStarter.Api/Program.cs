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
// Register handlers from API, Orders, and Inventory assemblies
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Program).Assembly,
    typeof(OrderAggregate).Assembly,
    typeof(ProductStockAggregate).Assembly
));

// FileEventStore
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "events");
builder.Services.AddFileEventStore(dataPath);

// Infrastructure - Distributed Tracing
builder.Services.AddScoped<CorrelationContext>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IIdempotencyService, InMemoryIdempotencyService>();

// Infrastructure - Event Publishing (with correlation ID enrichment)
builder.Services.AddScoped<MediatREventPublisher>();
builder.Services.AddScoped<IEventPublisher>(sp =>
{
    var inner = sp.GetRequiredService<MediatREventPublisher>();
    var correlationContext = sp.GetRequiredService<CorrelationContext>();
    var logger = sp.GetRequiredService<ILogger<CorrelationIdEventPublisher>>();
    return new CorrelationIdEventPublisher(inner, correlationContext, logger);
});

builder.Services.AddHostedService<ReadModelInitializer>();

// Modules
builder.Services.AddOrdersModule();
builder.Services.AddInventoryModule();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

// Middleware - Distributed Tracing
app.UseCorrelationId();

app.UseCors();
app.MapDefaultEndpoints();

// Module endpoints
app.MapOrderEndpoints();
app.MapInventoryEndpoints();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
