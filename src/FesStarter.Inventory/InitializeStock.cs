using FileEventStore.Session;
using FesStarter.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FesStarter.Inventory;

public record InitializeStockCommand(string ProductId, string ProductName, int InitialQuantity);
public record InitializeStockRequest(string ProductName, int InitialQuantity);

public class InitializeStockHandler(IEventSessionFactory sessionFactory, IEventPublisher eventPublisher, StockReadModel readModel)
{
    public async Task HandleAsync(InitializeStockCommand command)
    {
        await using var session = sessionFactory.OpenSession();

        var stock = await session.AggregateStreamOrCreateAsync<ProductStockAggregate>($"stock-{command.ProductId}");
        stock.Initialize(command.ProductId, command.ProductName, command.InitialQuantity);

        var events = stock.UncommittedEvents.ToList();
        await session.SaveChangesAsync();

        foreach (var evt in events) readModel.Apply(evt);
        await eventPublisher.PublishAsync(events);
    }
}

public static class InitializeStockEndpoint
{
    public static void Map(WebApplication app) =>
        app.MapPost("/api/products/{productId}/stock", async (string productId, InitializeStockRequest request, InitializeStockHandler handler) =>
        {
            await handler.HandleAsync(new InitializeStockCommand(productId, request.ProductName, request.InitialQuantity));
            return Results.Created($"/api/products/{productId}/stock", null);
        })
        .WithName("InitializeStock")
        .WithTags("Inventory");
}
