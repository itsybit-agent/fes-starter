using FileEventStore.Session;
using FesStarter.Core.Idempotency;
using FesStarter.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FesStarter.Inventory;

public record InitializeStockCommand(
    string ProductId,
    string ProductName,
    int InitialQuantity,
    string? IdempotencyKey = null) : IIdempotentCommand
{
    string IIdempotentCommand.IdempotencyKey =>
        IdempotencyKey ?? Guid.NewGuid().ToString();
}

public record InitializeStockRequest(string ProductName, int InitialQuantity);

public class InitializeStockHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher)
{
    public async Task<bool> HandleAsync(InitializeStockCommand command)
    {
        await using var session = sessionFactory.OpenSession();

        var stock = await session.AggregateStreamOrCreateAsync<ProductStockAggregate>($"{command.ProductId}");
        stock.Initialize(command.ProductId, command.ProductName, command.InitialQuantity);

        var events = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();
        await eventPublisher.PublishAsync(events);

        return true;
    }
}

public static class InitializeStockEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/products/{productId}/stock", async (
            string productId,
            HttpRequest request,
            InitializeStockRequest stockRequest,
            InitializeStockHandler handler,
            IIdempotencyService idempotencyService) =>
        {
            var idempotencyKey = request.GetIdempotencyKey();
            var command = new InitializeStockCommand(
                productId,
                stockRequest.ProductName,
                stockRequest.InitialQuantity,
                idempotencyKey);

            // Execute with idempotency enforcement
            await idempotencyService.GetOrExecuteAsync(
                idempotencyKey ?? "",
                () => handler.HandleAsync(command));

            return Results.Created($"/api/products/{productId}/stock", null);
        })
        .WithName("InitializeStock")
        .WithTags("Inventory");
}
