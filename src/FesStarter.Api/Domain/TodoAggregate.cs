using FileEventStore;
using FileEventStore.Aggregates;

namespace FesStarter.Api.Domain;

public class TodoAggregate : Aggregate
{
    public string Title { get; private set; } = "";
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public void Create(string id, string title)
    {
        if (!string.IsNullOrEmpty(Id))
            throw new InvalidOperationException("Todo already exists");

        Emit(new TodoCreated(id, title, DateTime.UtcNow));
    }

    public void Complete()
    {
        if (IsCompleted)
            throw new InvalidOperationException("Todo already completed");

        Emit(new TodoCompleted(Id, DateTime.UtcNow));
    }

    protected override void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case TodoCreated e:
                Id = e.TodoId;
                Title = e.Title;
                break;
            case TodoCompleted e:
                IsCompleted = true;
                CompletedAt = e.CompletedAt;
                break;
        }
    }
}

// Events
public record TodoCreated(string TodoId, string Title, DateTime CreatedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}

public record TodoCompleted(string TodoId, DateTime CompletedAt) : IStoreableEvent
{
    public string TimestampUtc { get; set; } = "";
}
