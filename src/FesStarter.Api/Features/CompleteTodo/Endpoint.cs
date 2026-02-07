namespace FesStarter.Api.Features.CompleteTodo;

public static class Endpoint
{
    public static void MapCompleteTodo(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/todos/{id}/complete", async (string id, CompleteTodoHandler handler) =>
        {
            await handler.HandleAsync(new CompleteTodoCommand(id));
            return Results.NoContent();
        })
        .WithName("CompleteTodo")
        .WithOpenApi();
    }
}
