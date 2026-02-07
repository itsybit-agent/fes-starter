using FileEventStore.Session;
using FesStarter.Api.Domain;
using FesStarter.Api.Features.ListTodos;

namespace FesStarter.Api.Features.CreateTodo;

public class CreateTodoHandler
{
    private readonly IEventSessionFactory _sessionFactory;
    private readonly TodoReadModel _readModel;

    public CreateTodoHandler(IEventSessionFactory sessionFactory, TodoReadModel readModel)
    {
        _sessionFactory = sessionFactory;
        _readModel = readModel;
    }

    public async Task<CreateTodoResponse> HandleAsync(CreateTodoCommand command)
    {
        var id = Guid.NewGuid().ToString();
        
        await using var session = _sessionFactory.OpenSession();
        
        var todo = await session.AggregateStreamOrCreateAsync<TodoAggregate>(id);
        todo.Create(id, command.Title);
        
        // Capture events before saving (they get cleared after)
        var events = todo.UncommittedEvents.ToList();
        
        await session.SaveChangesAsync();
        
        // Update read model
        foreach (var evt in events)
        {
            _readModel.Apply(evt);
        }
        
        return new CreateTodoResponse(id);
    }
}
