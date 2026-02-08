using FesStarter.Api.Infrastructure;
using FesStarter.Events;
using FesStarter.Events.Inventory;
using FesStarter.Inventory;
using MediatR;

namespace FesStarter.Api.Features.Projections;

/// <summary>
/// Projects inventory domain events to the StockReadModel.
/// This implements the eventually consistent read model pattern - events trigger async updates.
/// </summary>
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
