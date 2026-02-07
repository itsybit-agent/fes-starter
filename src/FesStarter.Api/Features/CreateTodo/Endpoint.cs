namespace FesStarter.Api.Features.CreateTodo;

public static class Endpoint
{
    public static void MapCreateTodo(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/todos", async (CreateTodoCommand command, CreateTodoHandler handler) =>
        {
            var response = await handler.HandleAsync(command);
            return Results.Created($"/api/todos/{response.Id}", response);
        })
        .WithName("CreateTodo")
        .WithOpenApi();
    }
}
