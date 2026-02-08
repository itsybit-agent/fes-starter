using System.Collections.Concurrent;
using FileEventStore;
using FesStarter.Events;
using FesStarter.Events.Inventory;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FesStarter.Inventory;

public record StockDto(string ProductId, string ProductName, int QuantityOnHand, int QuantityReserved, int AvailableQuantity);

public class StockReadModel
{
    private readonly ConcurrentDictionary<string, StockDto> _stocks = new();

    public void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case StockInitialized e:
                _stocks[e.ProductId] = new StockDto(e.ProductId, e.ProductName, e.InitialQuantity, 0, e.InitialQuantity);
                break;
            case StockReserved e:
                if (_stocks.TryGetValue(e.ProductId, out var toReserve))
                {
                    var newReserved = toReserve.QuantityReserved + e.Quantity;
                    _stocks[e.ProductId] = toReserve with { QuantityReserved = newReserved, AvailableQuantity = toReserve.QuantityOnHand - newReserved };
                }
                // If stock doesn't exist yet, it will be created when StockInitialized arrives
                break;
            case StockDeducted e:
                if (_stocks.TryGetValue(e.ProductId, out var toDeduct))
                {
                    var newOnHand = toDeduct.QuantityOnHand - e.Quantity;
                    var newReserved2 = toDeduct.QuantityReserved - e.Quantity;
                    _stocks[e.ProductId] = toDeduct with { QuantityOnHand = newOnHand, QuantityReserved = newReserved2, AvailableQuantity = newOnHand - newReserved2 };
                }
                // If stock doesn't exist yet, it will be created when StockInitialized arrives
                break;
            case StockRestocked e:
                if (_stocks.TryGetValue(e.ProductId, out var toRestock))
                {
                    var newOnHand = toRestock.QuantityOnHand + e.Quantity;
                    _stocks[e.ProductId] = toRestock with { QuantityOnHand = newOnHand, AvailableQuantity = newOnHand - toRestock.QuantityReserved };
                }
                // If stock doesn't exist yet, it will be created when StockInitialized arrives
                break;
        }
    }

    public List<StockDto> GetAll() => _stocks.Values.ToList();
    public StockDto? Get(string productId) => _stocks.GetValueOrDefault(productId);
}

public class StockReadModelProjections(StockReadModel readModel, ILogger<StockReadModelProjections> logger) :
    INotificationHandler<DomainEventNotification<StockInitialized>>,
    INotificationHandler<DomainEventNotification<StockReserved>>,
    INotificationHandler<DomainEventNotification<StockDeducted>>,
    INotificationHandler<DomainEventNotification<StockRestocked>>
{
    public Task Handle(DomainEventNotification<StockInitialized> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting StockInitialized: {ProductId}", notification.DomainEvent.ProductId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<StockReserved> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting StockReserved: {ProductId}", notification.DomainEvent.ProductId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<StockDeducted> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting StockDeducted: {ProductId}", notification.DomainEvent.ProductId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<StockRestocked> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting StockRestocked: {ProductId}", notification.DomainEvent.ProductId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }
}

public class GetStockHandler(StockReadModel readModel)
{
    public Task<StockDto?> HandleAsync(string productId) => Task.FromResult(readModel.Get(productId));
    public Task<List<StockDto>> HandleAllAsync() => Task.FromResult(readModel.GetAll());
}

public static class GetStockEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/products/{productId}/stock", async (string productId, GetStockHandler handler) =>
        {
            var stock = await handler.HandleAsync(productId);
            return stock is null ? Results.NotFound() : Results.Ok(stock);
        })
        .WithName("GetStock")
        .WithTags("Inventory");

        app.MapGet("/api/products/stock", async (GetStockHandler handler) => Results.Ok(await handler.HandleAllAsync()))
        .WithName("ListStock")
        .WithTags("Inventory");
    }
}
