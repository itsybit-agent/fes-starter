namespace FesStarter.Api.Features.Inventory.InitializeStock;

public record InitializeStockCommand(string ProductId, string ProductName, int InitialQuantity);
