namespace FesStarter.Api.Features.Orders.ListOrders;

public class ListOrdersHandler
{
    private readonly OrderReadModel _readModel;

    public ListOrdersHandler(OrderReadModel readModel)
    {
        _readModel = readModel;
    }

    public Task<List<OrderDto>> HandleAsync(ListOrdersQuery query)
    {
        return Task.FromResult(_readModel.GetAll());
    }
}
