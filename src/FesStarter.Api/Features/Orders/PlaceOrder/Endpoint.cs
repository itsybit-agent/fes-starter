namespace FesStarter.Api.Features.Orders.PlaceOrder;

public static class Endpoint
{
    public static void MapPlaceOrder(this WebApplication app)
    {
        app.MapPost("/api/orders", async (PlaceOrderCommand command, PlaceOrderHandler handler) =>
        {
            var response = await handler.HandleAsync(command);
            return Results.Created($"/api/orders/{response.OrderId}", response);
        })
        .WithName("PlaceOrder")
        .WithTags("Orders");
    }
}
