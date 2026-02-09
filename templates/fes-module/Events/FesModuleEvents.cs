using FileEventStore;
using FesStarter.Events;

namespace FesStarter.Events.FesModule;

// Add to FesStarter.Events project, not here
// This file is a reference for the events this module produces

public record FesModuleCreated(string Name) : IStoreableEvent, ICorrelatedEvent
{
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
}
