namespace FesStarter.Api.Features.Orders.ListOrders;

public static class Endpoint
{
    public static void MapListOrders(this WebApplication app)
    {
        app.MapGet("/api/orders", async (ListOrdersHandler handler) =>
        {
            var orders = await handler.HandleAsync(new ListOrdersQuery());
            return Results.Ok(orders);
        })
        .WithName("ListOrders")
        .WithTags("Orders");
    }
}
