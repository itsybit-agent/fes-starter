using FileEventStore.Session;
using FesStarter.Api.Domain.Orders;
using FesStarter.Api.Infrastructure;

namespace FesStarter.Api.Features.Orders.PlaceOrder;

public class PlaceOrderHandler
{
    private readonly IEventSessionFactory _sessionFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly OrderReadModel _readModel;

    public PlaceOrderHandler(
        IEventSessionFactory sessionFactory, 
        IEventPublisher eventPublisher,
        OrderReadModel readModel)
    {
        _sessionFactory = sessionFactory;
        _eventPublisher = eventPublisher;
        _readModel = readModel;
    }

    public async Task<PlaceOrderResponse> HandleAsync(PlaceOrderCommand command)
    {
        var orderId = Guid.NewGuid().ToString();
        
        await using var session = _sessionFactory.OpenSession();
        
        var order = await session.AggregateStreamOrCreateAsync<OrderAggregate>($"order-{orderId}");
        order.Place(orderId, command.ProductId, command.Quantity);
        
        // Capture events before saving
        var events = order.UncommittedEvents.ToList();
        
        await session.SaveChangesAsync();
        
        // Update read model
        foreach (var evt in events)
        {
            _readModel.Apply(evt);
        }
        
        // Publish for translations (cross-context reactions)
        await _eventPublisher.PublishAsync(events);
        
        return new PlaceOrderResponse(orderId);
    }
}
