using FileEventStore.Session;
using FesStarter.Domain.Orders;
using FesStarter.Api.Infrastructure;

namespace FesStarter.Api.Features.Orders;

public record PlaceOrderCommand(string ProductId, int Quantity);
public record PlaceOrderResponse(string OrderId);

public class PlaceOrderHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher, OrderReadModel readModel)
{
    public async Task<PlaceOrderResponse> HandleAsync(PlaceOrderCommand command)
    {
        var orderId = Guid.NewGuid().ToString();
        
        await using var session = sessionFactory.OpenSession();
        var order = await session.AggregateStreamOrCreateAsync<OrderAggregate>($"order-{orderId}");
        order.Place(orderId, command.ProductId, command.Quantity);
        
        var events = order.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        
        foreach (var evt in events) readModel.Apply(evt);
        await eventPublisher.PublishAsync(events);
        
        return new PlaceOrderResponse(orderId);
    }
}

public static class PlaceOrderEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/orders", async (PlaceOrderCommand command, PlaceOrderHandler handler) =>
        {
            var response = await handler.HandleAsync(command);
            return Results.Created($"/api/orders/{response.OrderId}", response);
        })
        .WithName("PlaceOrder")
        .WithTags("Orders");
}
