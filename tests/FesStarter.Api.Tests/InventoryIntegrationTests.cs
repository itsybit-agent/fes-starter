using System.Net;
using System.Net.Http.Json;
using FesStarter.Api.Tests.Infrastructure;
using FesStarter.Inventory;

namespace FesStarter.Api.Tests;

public class InventoryIntegrationTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;

    public InventoryIntegrationTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task InitializeStock_ReturnsCreated()
    {
        // Arrange
        var productId = $"product-{Guid.NewGuid()}";
        var request = new { ProductName = "Widget", InitialQuantity = 100 };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/stock", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetStock_AfterInitialization_ReturnsCorrectQuantity()
    {
        // Arrange
        var productId = $"product-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync($"/api/products/{productId}/stock", 
            new { ProductName = "Gadget", InitialQuantity = 50 });
        
        // Give time for read model to update
        await Task.Delay(100);

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}/stock");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = await response.Content.ReadFromJsonAsync<StockDto>();
        Assert.NotNull(stock);
        Assert.Equal(productId, stock.ProductId);
        Assert.Equal("Gadget", stock.ProductName);
        Assert.Equal(50, stock.QuantityOnHand);
        Assert.Equal(50, stock.AvailableQuantity);
    }

    [Fact]
    public async Task GetStock_ForNonExistentProduct_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/products/nonexistent-product/stock");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListStock_ReturnsAllProducts()
    {
        // Arrange - Create a few products
        var productId1 = $"list-test-{Guid.NewGuid()}";
        var productId2 = $"list-test-{Guid.NewGuid()}";
        
        await _client.PostAsJsonAsync($"/api/products/{productId1}/stock", 
            new { ProductName = "Product A", InitialQuantity = 10 });
        await _client.PostAsJsonAsync($"/api/products/{productId2}/stock", 
            new { ProductName = "Product B", InitialQuantity = 20 });
        
        await Task.Delay(100);

        // Act
        var response = await _client.GetAsync("/api/products/stock");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stocks = await response.Content.ReadFromJsonAsync<List<StockDto>>();
        Assert.NotNull(stocks);
        Assert.True(stocks.Count >= 2);
    }
}
