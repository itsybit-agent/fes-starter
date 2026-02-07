using System.Collections.Concurrent;
using FileEventStore;
using FesStarter.Api.Domain.Inventory;

namespace FesStarter.Api.Features.Inventory;

public class StockReadModel
{
    private readonly ConcurrentDictionary<string, StockDto> _stocks = new();

    public void Apply(IStoreableEvent evt)
    {
        switch (evt)
        {
            case StockInitialized e:
                _stocks[e.ProductId] = new StockDto(
                    e.ProductId, 
                    e.ProductName, 
                    e.InitialQuantity, 
                    0,
                    e.InitialQuantity);
                break;
            case StockReserved e:
                if (_stocks.TryGetValue(e.ProductId, out var stockToReserve))
                {
                    var newReserved = stockToReserve.QuantityReserved + e.Quantity;
                    _stocks[e.ProductId] = stockToReserve with 
                    { 
                        QuantityReserved = newReserved,
                        AvailableQuantity = stockToReserve.QuantityOnHand - newReserved
                    };
                }
                break;
            case StockDeducted e:
                if (_stocks.TryGetValue(e.ProductId, out var stockToDeduct))
                {
                    var newOnHand = stockToDeduct.QuantityOnHand - e.Quantity;
                    var newReserved2 = stockToDeduct.QuantityReserved - e.Quantity;
                    _stocks[e.ProductId] = stockToDeduct with 
                    { 
                        QuantityOnHand = newOnHand,
                        QuantityReserved = newReserved2,
                        AvailableQuantity = newOnHand - newReserved2
                    };
                }
                break;
            case StockRestocked e:
                if (_stocks.TryGetValue(e.ProductId, out var stockToRestock))
                {
                    var newOnHand2 = stockToRestock.QuantityOnHand + e.Quantity;
                    _stocks[e.ProductId] = stockToRestock with 
                    { 
                        QuantityOnHand = newOnHand2,
                        AvailableQuantity = newOnHand2 - stockToRestock.QuantityReserved
                    };
                }
                break;
        }
    }

    public List<StockDto> GetAll() => _stocks.Values.ToList();
    
    public StockDto? Get(string productId) => _stocks.GetValueOrDefault(productId);
}

public record StockDto(
    string ProductId, 
    string ProductName, 
    int QuantityOnHand, 
    int QuantityReserved,
    int AvailableQuantity);
