namespace FesStarter.Api.Features.CreateTodo;

public record CreateTodoCommand(string Title);

public record CreateTodoResponse(string Id);
