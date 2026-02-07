namespace FesStarter.Api.Features.Orders.PlaceOrder;

public record PlaceOrderCommand(string ProductId, int Quantity);
public record PlaceOrderResponse(string OrderId);
