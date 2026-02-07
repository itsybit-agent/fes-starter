using FileEventStore.Session;
using FesStarter.Api.Domain.Orders;
using FesStarter.Api.Infrastructure;

namespace FesStarter.Api.Features.Orders.ShipOrder;

public class ShipOrderHandler
{
    private readonly IEventSessionFactory _sessionFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly OrderReadModel _readModel;

    public ShipOrderHandler(
        IEventSessionFactory sessionFactory, 
        IEventPublisher eventPublisher,
        OrderReadModel readModel)
    {
        _sessionFactory = sessionFactory;
        _eventPublisher = eventPublisher;
        _readModel = readModel;
    }

    public async Task HandleAsync(ShipOrderCommand command)
    {
        await using var session = _sessionFactory.OpenSession();
        
        var order = await session.AggregateStreamAsync<OrderAggregate>($"order-{command.OrderId}");
        if (order == null)
            throw new InvalidOperationException($"Order {command.OrderId} not found");
        
        order.Ship();
        
        var events = order.UncommittedEvents.ToList();
        
        await session.SaveChangesAsync();
        
        foreach (var evt in events)
        {
            _readModel.Apply(evt);
        }
        
        await _eventPublisher.PublishAsync(events);
    }
}
