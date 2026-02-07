namespace FesStarter.Api.Features.Orders.ShipOrder;

public static class Endpoint
{
    public static void MapShipOrder(this WebApplication app)
    {
        app.MapPost("/api/orders/{orderId}/ship", async (string orderId, ShipOrderHandler handler) =>
        {
            await handler.HandleAsync(new ShipOrderCommand(orderId));
            return Results.Ok();
        })
        .WithName("ShipOrder")
        .WithTags("Orders");
    }
}
