namespace FesStarter.Api.Features.Inventory.GetStock;

public class GetStockHandler
{
    private readonly StockReadModel _readModel;

    public GetStockHandler(StockReadModel readModel)
    {
        _readModel = readModel;
    }

    public Task<StockDto?> HandleAsync(GetStockQuery query)
    {
        return Task.FromResult(_readModel.Get(query.ProductId));
    }
    
    public Task<List<StockDto>> HandleAsync(ListStockQuery query)
    {
        return Task.FromResult(_readModel.GetAll());
    }
}
