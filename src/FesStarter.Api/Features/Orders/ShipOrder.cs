using FileEventStore.Session;
using FesStarter.Domain.Orders;
using FesStarter.Api.Infrastructure;

namespace FesStarter.Api.Features.Orders;

public record ShipOrderCommand(string OrderId);

public class ShipOrderHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher, OrderReadModel readModel)
{
    public async Task HandleAsync(ShipOrderCommand command)
    {
        await using var session = sessionFactory.OpenSession();
        
        var order = await session.AggregateStreamAsync<OrderAggregate>($"order-{command.OrderId}")
            ?? throw new InvalidOperationException($"Order {command.OrderId} not found");
        
        order.Ship();
        
        var events = order.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        
        foreach (var evt in events) readModel.Apply(evt);
        await eventPublisher.PublishAsync(events);
    }
}

public static class ShipOrderEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/orders/{orderId}/ship", async (string orderId, ShipOrderHandler handler) =>
        {
            await handler.HandleAsync(new ShipOrderCommand(orderId));
            return Results.Ok();
        })
        .WithName("ShipOrder")
        .WithTags("Orders");
}
