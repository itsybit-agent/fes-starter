using FileEventStore.Session;
using FesStarter.Core.Idempotency;
using FesStarter.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FesStarter.Orders;

public record PlaceOrderCommand(string ProductId, int Quantity, string? IdempotencyKey = null) : IIdempotentCommand
{
    string IIdempotentCommand.IdempotencyKey =>
        IdempotencyKey ?? Guid.NewGuid().ToString();
}

public record PlaceOrderResponse(string OrderId);

public class PlaceOrderHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher)
{
    public async Task<PlaceOrderResponse> HandleAsync(PlaceOrderCommand command)
    {
        var orderId = Guid.NewGuid().ToString();

        await using var session = sessionFactory.OpenSession();
        var order = await session.AggregateStreamOrCreateAsync<OrderAggregate>(orderId);
        order.Place(orderId, command.ProductId, command.Quantity);

        var events = order.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events);

        return new PlaceOrderResponse(orderId);
    }
}

public static class PlaceOrderEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/orders", async (
            HttpRequest request,
            PlaceOrderCommand command,
            PlaceOrderHandler handler,
            IIdempotencyService idempotencyService) =>
        {
            // Extract idempotency key from headers
            var idempotencyKey = request.GetIdempotencyKey();
            var commandWithKey = command with { IdempotencyKey = idempotencyKey };

            // Execute with idempotency enforcement - cached results for duplicate requests
            var response = await idempotencyService.GetOrExecuteAsync(
                idempotencyKey ?? "",
                () => handler.HandleAsync(commandWithKey));

            return Results.Created($"/api/orders/{response?.OrderId}", response);
        })
        .WithName("PlaceOrder")
        .WithTags("Orders");
}
