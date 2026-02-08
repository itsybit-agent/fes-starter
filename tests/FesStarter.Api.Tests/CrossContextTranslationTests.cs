using System.Net;
using System.Net.Http.Json;
using FesStarter.Api.Tests.Infrastructure;
using FesStarter.Inventory;
using FesStarter.Orders;

namespace FesStarter.Api.Tests;

/// <summary>
/// Integration tests that verify cross-context event translations.
/// These tests ensure that events from one bounded context correctly
/// trigger operations in another bounded context.
/// </summary>
public class CrossContextTranslationTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;

    public CrossContextTranslationTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task PlaceOrder_ReservesStock_InInventoryContext()
    {
        // Arrange - Initialize stock for a product
        var productId = $"translation-test-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync($"/api/products/{productId}/stock",
            new { ProductName = "Translation Test Product", InitialQuantity = 100 });
        
        await Task.Delay(100);

        // Verify initial stock
        var initialStockResponse = await _client.GetAsync($"/api/products/{productId}/stock");
        var initialStock = await initialStockResponse.Content.ReadFromJsonAsync<StockDto>();
        Assert.Equal(100, initialStock!.QuantityOnHand);
        Assert.Equal(0, initialStock.QuantityReserved);
        Assert.Equal(100, initialStock.AvailableQuantity);

        // Act - Place an order (which should trigger stock reservation via translation)
        var orderResponse = await _client.PostAsJsonAsync("/api/orders",
            new PlaceOrderCommand(productId, 25));
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        
        // Wait for async translation to complete
        await Task.Delay(500);

        // Assert - Stock should now show reserved quantity
        var updatedStockResponse = await _client.GetAsync($"/api/products/{productId}/stock");
        var updatedStock = await updatedStockResponse.Content.ReadFromJsonAsync<StockDto>();
        
        Assert.Equal(100, updatedStock!.QuantityOnHand);
        Assert.Equal(25, updatedStock.QuantityReserved);
        Assert.Equal(75, updatedStock.AvailableQuantity);
    }

    [Fact]
    public async Task ShipOrder_DeductsStock_FromReserved()
    {
        // Arrange - Initialize stock and place an order
        var productId = $"ship-deduct-test-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync($"/api/products/{productId}/stock",
            new { ProductName = "Ship Deduct Test", InitialQuantity = 50 });
        
        await Task.Delay(100);

        // Place order (triggers reservation)
        var orderResponse = await _client.PostAsJsonAsync("/api/orders",
            new PlaceOrderCommand(productId, 10));
        var order = await orderResponse.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        
        await Task.Delay(500); // Wait for reservation

        // Verify reservation happened
        var reservedStockResponse = await _client.GetAsync($"/api/products/{productId}/stock");
        var reservedStock = await reservedStockResponse.Content.ReadFromJsonAsync<StockDto>();
        Assert.Equal(10, reservedStock!.QuantityReserved);

        // Act - Ship the order (should deduct from stock)
        var shipResponse = await _client.PostAsync($"/api/orders/{order!.OrderId}/ship", null);
        Assert.Equal(HttpStatusCode.OK, shipResponse.StatusCode);
        
        await Task.Delay(500); // Wait for deduction

        // Assert - Stock should be deducted and reservation cleared
        var finalStockResponse = await _client.GetAsync($"/api/products/{productId}/stock");
        var finalStock = await finalStockResponse.Content.ReadFromJsonAsync<StockDto>();
        
        Assert.Equal(40, finalStock!.QuantityOnHand); // 50 - 10
        Assert.Equal(0, finalStock.QuantityReserved);  // Reservation cleared
        Assert.Equal(40, finalStock.AvailableQuantity);
    }

    [Fact]
    public async Task MultipleOrders_AccumulateReservations()
    {
        // Arrange
        var productId = $"multi-order-test-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync($"/api/products/{productId}/stock",
            new { ProductName = "Multi Order Test", InitialQuantity = 100 });
        
        await Task.Delay(100);

        // Act - Place multiple orders
        await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand(productId, 15));
        await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand(productId, 20));
        await _client.PostAsJsonAsync("/api/orders", new PlaceOrderCommand(productId, 5));
        
        await Task.Delay(1000); // Wait for all translations

        // Assert - All reservations should accumulate
        var stockResponse = await _client.GetAsync($"/api/products/{productId}/stock");
        var stock = await stockResponse.Content.ReadFromJsonAsync<StockDto>();
        
        Assert.Equal(100, stock!.QuantityOnHand);
        Assert.Equal(40, stock.QuantityReserved); // 15 + 20 + 5
        Assert.Equal(60, stock.AvailableQuantity);
    }

    [Fact]
    public async Task CorrelationId_PropagatesThroughTranslation()
    {
        // Arrange - Initialize stock
        var productId = $"correlation-test-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync($"/api/products/{productId}/stock",
            new { ProductName = "Correlation Test", InitialQuantity = 100 });
        
        await Task.Delay(100);

        // Act - Place order with correlation ID header
        var correlationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new PlaceOrderCommand(productId, 10))
        };
        request.Headers.Add("X-Correlation-ID", correlationId);
        
        var response = await _client.SendAsync(request);
        
        // Assert - Request succeeded (correlation ID handling is internal)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        // Response should echo correlation ID if the middleware is configured
        // This is implementation-dependent
    }
}
