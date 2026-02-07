namespace FesStarter.Api.Features.ListTodos;

public record TodoDto(string Id, string Title, bool IsCompleted, DateTime CreatedAt, DateTime? CompletedAt);

public record ListTodosResponse(IReadOnlyList<TodoDto> Todos);
