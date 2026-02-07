using FileEventStore;
using FesStarter.Api.Domain;
using FesStarter.Api.Features.CreateTodo;
using FesStarter.Api.Features.CompleteTodo;
using FesStarter.Api.Features.ListTodos;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// FileEventStore
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "events");
builder.Services.AddFileEventStore(dataPath);

// Read model (singleton - in production, use a proper database)
builder.Services.AddSingleton<TodoReadModel>();
builder.Services.AddHostedService<ReadModelInitializer>();

// Handlers
builder.Services.AddScoped<CreateTodoHandler>();
builder.Services.AddScoped<CompleteTodoHandler>();
builder.Services.AddScoped<ListTodosHandler>();

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
app.MapCreateTodo();
app.MapCompleteTodo();
app.MapListTodos();

app.Run();
