using FileEventStore;

namespace FesStarter.Events;

public interface IEventPublisher
{
    Task PublishAsync(IEnumerable<IStoreableEvent> events, CancellationToken ct = default);
}
