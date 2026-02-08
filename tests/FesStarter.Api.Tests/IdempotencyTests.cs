using System.Net;
using System.Net.Http.Json;
using FesStarter.Api.Tests.Infrastructure;
using FesStarter.Inventory;
using FesStarter.Orders;

namespace FesStarter.Api.Tests;

/// <summary>
/// Tests for idempotency enforcement across endpoints.
/// Verifies that duplicate requests with same idempotency key
/// return cached results without re-executing commands.
/// </summary>
public class IdempotencyTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;

    public IdempotencyTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task PlaceOrder_SameIdempotencyKey_ReturnsSameOrderId()
    {
        // Arrange
        var productId = $"idem-order-{Guid.NewGuid()}";
        await InitializeStock(productId, 100);
        
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new PlaceOrderCommand(productId, 5);

        // Act - Send same request twice with same idempotency key
        var response1 = await PlaceOrderWithKey(command, idempotencyKey);
        var response2 = await PlaceOrderWithKey(command, idempotencyKey);

        // Assert - Both succeed with same OrderId
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        
        var order1 = await response1.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        var order2 = await response2.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        
        Assert.NotNull(order1);
        Assert.NotNull(order2);
        Assert.Equal(order1.OrderId, order2.OrderId);
    }

    [Fact]
    public async Task PlaceOrder_DifferentIdempotencyKeys_CreatesDifferentOrders()
    {
        // Arrange
        var productId = $"idem-diff-{Guid.NewGuid()}";
        await InitializeStock(productId, 100);
        
        var command = new PlaceOrderCommand(productId, 5);

        // Act - Send same command with different keys
        var response1 = await PlaceOrderWithKey(command, Guid.NewGuid().ToString());
        var response2 = await PlaceOrderWithKey(command, Guid.NewGuid().ToString());

        // Assert - Different OrderIds (both executed)
        var order1 = await response1.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        var order2 = await response2.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        
        Assert.NotEqual(order1!.OrderId, order2!.OrderId);
    }

    [Fact]
    public async Task PlaceOrder_NoIdempotencyKey_CreatesDuplicates()
    {
        // Arrange
        var productId = $"idem-nokey-{Guid.NewGuid()}";
        await InitializeStock(productId, 100);
        
        var command = new PlaceOrderCommand(productId, 5);

        // Act - Send same command without idempotency key
        var response1 = await _client.PostAsJsonAsync("/api/orders", command);
        var response2 = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert - Different OrderIds (no idempotency enforcement)
        var order1 = await response1.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        var order2 = await response2.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        
        Assert.NotEqual(order1!.OrderId, order2!.OrderId);
    }

    [Fact]
    public async Task PlaceOrder_SameKeyDifferentQuantity_ReturnsFirstResult()
    {
        // Arrange - Same key but different command payload
        var productId = $"idem-payload-{Guid.NewGuid()}";
        await InitializeStock(productId, 100);
        
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act - First with quantity 5, second with quantity 10
        var response1 = await PlaceOrderWithKey(new PlaceOrderCommand(productId, 5), idempotencyKey);
        var response2 = await PlaceOrderWithKey(new PlaceOrderCommand(productId, 10), idempotencyKey);

        // Assert - Returns cached first result (same OrderId)
        var order1 = await response1.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        var order2 = await response2.Content.ReadFromJsonAsync<PlaceOrderResponse>();
        
        Assert.Equal(order1!.OrderId, order2!.OrderId);
    }

    [Fact]
    public async Task InitializeStock_SameIdempotencyKey_DoesNotThrowOnSecondCall()
    {
        // Arrange
        var productId = $"idem-stock-{Guid.NewGuid()}";
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act - Initialize same stock twice with same idempotency key
        var response1 = await InitializeStockWithKey(productId, "Test Product", 50, idempotencyKey);
        var response2 = await InitializeStockWithKey(productId, "Test Product", 50, idempotencyKey);

        // Assert - Both succeed (second returns cached, doesn't re-execute)
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
    }

    [Fact]
    public async Task InitializeStock_NoIdempotencyKey_FailsOnSecondCall()
    {
        // Arrange
        var productId = $"idem-stock-throw-{Guid.NewGuid()}";

        // Act - Initialize same stock twice without idempotency key
        var response1 = await _client.PostAsJsonAsync(
            $"/api/products/{productId}/stock",
            new { ProductName = "Test", InitialQuantity = 50 });
        
        // Assert - First succeeds
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        
        // Second call should throw (aggregate already initialized)
        // TestHost propagates exceptions rather than returning HTTP 500
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _client.PostAsJsonAsync(
                $"/api/products/{productId}/stock",
                new { ProductName = "Test", InitialQuantity = 50 });
        });
        
        Assert.Contains("already initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #region Helpers

    private async Task InitializeStock(string productId, int quantity)
    {
        await _client.PostAsJsonAsync(
            $"/api/products/{productId}/stock",
            new { ProductName = "Test Product", InitialQuantity = quantity });
        await Task.Delay(100); // Wait for read model
    }

    private async Task<HttpResponseMessage> PlaceOrderWithKey(PlaceOrderCommand command, string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(command)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> InitializeStockWithKey(
        string productId, string productName, int quantity, string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{productId}/stock")
        {
            Content = JsonContent.Create(new { ProductName = productName, InitialQuantity = quantity })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _client.SendAsync(request);
    }

    #endregion
}
