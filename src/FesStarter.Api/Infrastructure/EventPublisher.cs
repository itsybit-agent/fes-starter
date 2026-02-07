using FileEventStore;
using MediatR;

namespace FesStarter.Api.Infrastructure;

public record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification where TEvent : IStoreableEvent;

public interface IEventPublisher
{
    Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct = default);
}

public class MediatREventPublisher(IMediator mediator, ILogger<MediatREventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct = default)
    {
        foreach (var evt in events)
        {
            logger.LogDebug("Publishing {EventType}", evt.GetType().Name);
            
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(evt.GetType());
            var notification = Activator.CreateInstance(notificationType, evt) as INotification;
            
            if (notification != null)
                await mediator.Publish(notification, ct);
        }
    }
}
