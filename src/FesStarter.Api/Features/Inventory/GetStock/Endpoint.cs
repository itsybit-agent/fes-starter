namespace FesStarter.Api.Features.Inventory.GetStock;

public static class Endpoint
{
    public static void MapGetStock(this WebApplication app)
    {
        app.MapGet("/api/products/{productId}/stock", async (string productId, GetStockHandler handler) =>
        {
            var stock = await handler.HandleAsync(new GetStockQuery(productId));
            return stock is null ? Results.NotFound() : Results.Ok(stock);
        })
        .WithName("GetStock")
        .WithTags("Inventory");
        
        app.MapGet("/api/products/stock", async (GetStockHandler handler) =>
        {
            var stocks = await handler.HandleAsync(new ListStockQuery());
            return Results.Ok(stocks);
        })
        .WithName("ListStock")
        .WithTags("Inventory");
    }
}
