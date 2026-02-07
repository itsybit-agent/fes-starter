using FileEventStore.Session;
using FesStarter.Api.Domain;

namespace FesStarter.Api.Features.CreateTodo;

public class CreateTodoHandler
{
    private readonly IEventSessionFactory _sessionFactory;

    public CreateTodoHandler(IEventSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    public async Task<CreateTodoResponse> HandleAsync(CreateTodoCommand command)
    {
        var id = Guid.NewGuid().ToString();
        
        await using var session = _sessionFactory.OpenSession();
        
        var todo = await session.AggregateStreamOrCreateAsync<TodoAggregate>(id);
        todo.Create(id, command.Title);
        
        await session.SaveChangesAsync();
        
        return new CreateTodoResponse(id);
    }
}
