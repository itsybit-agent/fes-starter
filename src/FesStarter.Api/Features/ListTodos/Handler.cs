namespace FesStarter.Api.Features.ListTodos;

public class ListTodosHandler
{
    private readonly TodoReadModel _readModel;

    public ListTodosHandler(TodoReadModel readModel)
    {
        _readModel = readModel;
    }

    public ListTodosResponse Handle()
    {
        return new ListTodosResponse(_readModel.GetAll());
    }
}
