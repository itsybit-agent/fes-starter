using System.Collections.Concurrent;
using FesStarter.Api.Domain;

namespace FesStarter.Api.Features.ListTodos;

/// <summary>
/// Simple in-memory read model for todos.
/// In production, this would be backed by a database and updated via event subscriptions.
/// </summary>
public class TodoReadModel
{
    private readonly ConcurrentDictionary<string, TodoDto> _todos = new();

    public void Apply(TodoCreated evt)
    {
        _todos[evt.TodoId] = new TodoDto(evt.TodoId, evt.Title, false, evt.CreatedAt, null);
    }

    public void Apply(TodoCompleted evt)
    {
        if (_todos.TryGetValue(evt.TodoId, out var todo))
        {
            _todos[evt.TodoId] = todo with { IsCompleted = true, CompletedAt = evt.CompletedAt };
        }
    }

    public IReadOnlyList<TodoDto> GetAll() => _todos.Values.ToList();
    
    public TodoDto? GetById(string id) => _todos.GetValueOrDefault(id);
}
