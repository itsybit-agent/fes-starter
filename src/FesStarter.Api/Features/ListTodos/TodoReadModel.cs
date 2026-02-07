using System.Collections.Concurrent;
using FesStarter.Api.Domain;

namespace FesStarter.Api.Features.ListTodos;

/// <summary>
/// In-memory read model for todos.
/// Updated inline by command handlers after events are saved.
/// In production, you'd persist this to a database.
/// </summary>
public class TodoReadModel
{
    private readonly ConcurrentDictionary<string, TodoDto> _todos = new();

    public void Apply(object evt)
    {
        switch (evt)
        {
            case TodoCreated created:
                _todos[created.TodoId] = new TodoDto(
                    created.TodoId, 
                    created.Title, 
                    false, 
                    created.CreatedAt, 
                    null);
                break;
                
            case TodoCompleted completed:
                if (_todos.TryGetValue(completed.TodoId, out var todo))
                {
                    _todos[completed.TodoId] = todo with 
                    { 
                        IsCompleted = true, 
                        CompletedAt = completed.CompletedAt 
                    };
                }
                break;
        }
    }

    public IReadOnlyList<TodoDto> GetAll() => _todos.Values.ToList();
    
    public TodoDto? GetById(string id) => _todos.GetValueOrDefault(id);
}
