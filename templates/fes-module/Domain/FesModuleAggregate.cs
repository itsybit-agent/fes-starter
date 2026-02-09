using FileEventStore;
using FesStarter.Events.FesModule;

namespace FesStarter.FesModule.Domain;

public class FesModuleAggregate : Aggregate
{
    // State properties
    public string Name { get; private set; } = string.Empty;

    // Command methods
    public void Create(string name)
    {
        if (Version > 0)
            throw new InvalidOperationException("Already created");

        Emit(new FesModuleCreated(name));
    }

    // Event handlers
    private void Apply(FesModuleCreated e)
    {
        Name = e.Name;
    }
}
