using FileEventStore.Session;
using FesStarter.Api.Domain;

namespace FesStarter.Api.Features.CompleteTodo;

public class CompleteTodoHandler
{
    private readonly IEventSessionFactory _sessionFactory;

    public CompleteTodoHandler(IEventSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    public async Task HandleAsync(CompleteTodoCommand command)
    {
        await using var session = _sessionFactory.OpenSession();
        
        var todo = await session.AggregateStreamAsync<TodoAggregate>(command.Id)
            ?? throw new InvalidOperationException($"Todo {command.Id} not found");
        
        todo.Complete();
        
        await session.SaveChangesAsync();
    }
}
