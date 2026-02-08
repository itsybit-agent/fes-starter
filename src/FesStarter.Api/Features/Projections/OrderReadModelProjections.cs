using FesStarter.Api.Infrastructure;
using FesStarter.Events;
using FesStarter.Events.Orders;
using FesStarter.Orders;
using MediatR;

namespace FesStarter.Api.Features.Projections;

/// <summary>
/// Projects order domain events to the OrderReadModel.
/// This implements the eventually consistent read model pattern - events trigger async updates.
/// </summary>
public class OrderReadModelProjections(OrderReadModel readModel, ILogger<OrderReadModelProjections> logger) :
    INotificationHandler<DomainEventNotification<OrderPlaced>>,
    INotificationHandler<DomainEventNotification<OrderStockReserved>>,
    INotificationHandler<DomainEventNotification<OrderShipped>>
{
    public Task Handle(DomainEventNotification<OrderPlaced> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting OrderPlaced to read model: {OrderId}", notification.DomainEvent.OrderId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<OrderStockReserved> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting OrderStockReserved to read model: {OrderId}", notification.DomainEvent.OrderId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<OrderShipped> notification, CancellationToken ct)
    {
        logger.LogDebug("Projecting OrderShipped to read model: {OrderId}", notification.DomainEvent.OrderId);
        readModel.Apply(notification.DomainEvent);
        return Task.CompletedTask;
    }
}
