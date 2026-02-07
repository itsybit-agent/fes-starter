using FileEventStore.Session;
using FesStarter.Api.Domain;
using FesStarter.Api.Features.ListTodos;

namespace FesStarter.Api.Features.CompleteTodo;

public class CompleteTodoHandler
{
    private readonly IEventSessionFactory _sessionFactory;
    private readonly TodoReadModel _readModel;

    public CompleteTodoHandler(IEventSessionFactory sessionFactory, TodoReadModel readModel)
    {
        _sessionFactory = sessionFactory;
        _readModel = readModel;
    }

    public async Task HandleAsync(CompleteTodoCommand command)
    {
        await using var session = _sessionFactory.OpenSession();
        
        var todo = await session.AggregateStreamAsync<TodoAggregate>(command.Id)
            ?? throw new InvalidOperationException($"Todo {command.Id} not found");
        
        todo.Complete();
        
        // Capture events before saving
        var events = todo.UncommittedEvents.ToList();
        
        await session.SaveChangesAsync();
        
        // Update read model
        foreach (var evt in events)
        {
            _readModel.Apply(evt);
        }
    }
}
