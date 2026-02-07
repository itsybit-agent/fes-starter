namespace FesStarter.Api.Features.Inventory.InitializeStock;

public static class Endpoint
{
    public static void MapInitializeStock(this WebApplication app)
    {
        app.MapPost("/api/products/{productId}/stock", async (
            string productId, 
            InitializeStockRequest request, 
            InitializeStockHandler handler) =>
        {
            await handler.HandleAsync(new InitializeStockCommand(productId, request.ProductName, request.InitialQuantity));
            return Results.Created($"/api/products/{productId}/stock", null);
        })
        .WithName("InitializeStock")
        .WithTags("Inventory");
    }
}

public record InitializeStockRequest(string ProductName, int InitialQuantity);
