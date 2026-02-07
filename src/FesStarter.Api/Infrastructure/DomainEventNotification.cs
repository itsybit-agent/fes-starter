using FileEventStore;
using MediatR;

namespace FesStarter.Api.Infrastructure;

/// <summary>
/// Wraps a domain event as a MediatR notification for pub/sub.
/// </summary>
public record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification 
    where TEvent : IStoreableEvent;

/// <summary>
/// Publishes domain events via MediatR after they're saved.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct = default);
}

public class MediatREventPublisher : IEventPublisher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatREventPublisher> _logger;

    public MediatREventPublisher(IMediator mediator, ILogger<MediatREventPublisher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct = default)
    {
        foreach (var evt in events)
        {
            _logger.LogDebug("Publishing domain event: {EventType}", evt.GetType().Name);
            
            // Create the generic notification type dynamically
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(evt.GetType());
            var notification = Activator.CreateInstance(notificationType, evt) as INotification;
            
            if (notification != null)
            {
                await _mediator.Publish(notification, ct);
            }
        }
    }
}
