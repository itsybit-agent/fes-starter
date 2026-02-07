namespace FesStarter.Api.Features.ListTodos;

public static class Endpoint
{
    public static void MapListTodos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/todos", (ListTodosHandler handler) =>
        {
            return Results.Ok(handler.Handle());
        })
        .WithName("ListTodos")
        .WithOpenApi();
    }
}
