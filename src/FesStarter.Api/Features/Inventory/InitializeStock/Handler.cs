using FileEventStore.Session;
using FesStarter.Api.Domain.Inventory;
using FesStarter.Api.Infrastructure;

namespace FesStarter.Api.Features.Inventory.InitializeStock;

public class InitializeStockHandler
{
    private readonly IEventSessionFactory _sessionFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly StockReadModel _readModel;

    public InitializeStockHandler(
        IEventSessionFactory sessionFactory, 
        IEventPublisher eventPublisher,
        StockReadModel readModel)
    {
        _sessionFactory = sessionFactory;
        _eventPublisher = eventPublisher;
        _readModel = readModel;
    }

    public async Task HandleAsync(InitializeStockCommand command)
    {
        await using var session = _sessionFactory.OpenSession();
        
        var stock = await session.AggregateStreamOrCreateAsync<ProductStockAggregate>($"stock-{command.ProductId}");
        stock.Initialize(command.ProductId, command.ProductName, command.InitialQuantity);
        
        var events = stock.UncommittedEvents.ToList();
        
        await session.SaveChangesAsync();
        
        foreach (var evt in events)
        {
            _readModel.Apply(evt);
        }
        
        await _eventPublisher.PublishAsync(events);
    }
}
