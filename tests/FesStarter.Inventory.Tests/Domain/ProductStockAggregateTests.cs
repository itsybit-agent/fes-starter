using FesStarter.Events.Inventory;
using FesStarter.Inventory;

namespace FesStarter.Inventory.Tests.Domain;

public class ProductStockAggregateTests
{
    private const string ProductId = "product-789";
    private const string ProductName = "Test Product";

    #region Initialize Stock Tests

    [Fact]
    public void Initialize_WithValidQuantity_EmitsStockInitializedEvent()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        var quantity = 100;

        // Act
        stock.Initialize(ProductId, ProductName, quantity);

        // Assert
        stock.UncommittedEvents.Should().HaveCount(1);
        var @event = stock.UncommittedEvents.First().Should().BeOfType<StockInitialized>().Subject;
        @event.ProductId.Should().Be(ProductId);
        @event.ProductName.Should().Be(ProductName);
        @event.InitialQuantity.Should().Be(quantity);
    }

    [Fact]
    public void Initialize_WithZeroQuantity_Succeeds()
    {
        // Arrange
        var stock = new ProductStockAggregate();

        // Act - Zero is allowed (not negative)
        stock.Initialize(ProductId, ProductName, 0);

        // Assert
        stock.QuantityOnHand.Should().Be(0);
    }

    [Fact]
    public void Initialize_WithNegativeQuantity_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => stock.Initialize(ProductId, ProductName, -10));
        ex.Message.Should().Contain("Initial quantity cannot be negative");
    }

    [Fact]
    public void Initialize_TwiceOnSameAggregate_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 50);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Initialize(ProductId, ProductName, 100));
        ex.Message.Should().Contain("Stock already initialized");
    }

    #endregion

    #region Reserve Stock Tests

    [Fact]
    public void Reserve_WithSufficientStock_EmitsStockReservedEvent()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        var quantity = 30;
        var orderId = "order-123";
        var beforeCount = stock.UncommittedEvents.Count;

        // Act
        stock.Reserve(quantity, orderId);

        // Assert
        stock.UncommittedEvents.Count.Should().Be(beforeCount + 1);
        var @event = stock.UncommittedEvents.Last().Should().BeOfType<StockReserved>().Subject;
        @event.ProductId.Should().Be(ProductId);
        @event.Quantity.Should().Be(quantity);
        @event.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Reserve_WithInsufficientStock_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 20);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Reserve(30, "order-123"));
        ex.Message.Should().Contain("Insufficient stock");
    }

    [Fact]
    public void Reserve_BeforeInitialize_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Reserve(10, "order-123"));
        ex.Message.Should().Contain("Stock not initialized");
    }

    [Fact]
    public void Reserve_WithZeroQuantity_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 50);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => stock.Reserve(0, "order-123"));
        ex.Message.Should().Contain("Quantity must be positive");
    }

    [Fact]
    public void Reserve_MultipleOrders_ReducesAvailableStock()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);

        // Act
        stock.Reserve(30, "order-1");
        stock.Reserve(20, "order-2");
        stock.Reserve(25, "order-3");

        // Assert - Available should be 100 - 30 - 20 - 25 = 25
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Reserve(26, "order-4"));
        ex.Message.Should().Contain("Insufficient stock");
    }

    [Fact]
    public void Reserve_ToExactLimit_Succeeds()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);

        // Act
        stock.Reserve(100, "order-1");

        // Assert
        // Next reserve should fail
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Reserve(1, "order-2"));
        ex.Message.Should().Contain("Insufficient stock");
    }

    #endregion

    #region Deduct Stock Tests

    [Fact]
    public void Deduct_AfterReservation_EmitsStockDeductedEvent()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(30, "order-123");
        var beforeCount = stock.UncommittedEvents.Count;

        // Act
        stock.Deduct(30, "order-123");

        // Assert
        stock.UncommittedEvents.Count.Should().Be(beforeCount + 1);
        var @event = stock.UncommittedEvents.Last().Should().BeOfType<StockDeducted>().Subject;
        @event.ProductId.Should().Be(ProductId);
        @event.Quantity.Should().Be(30);
        @event.OrderId.Should().Be("order-123");
    }

    [Fact]
    public void Deduct_BeforeInitialize_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Deduct(10, "order-123"));
        ex.Message.Should().Contain("Stock not initialized");
    }

    [Fact]
    public void Deduct_WithoutReservation_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Deduct(10, "order-123"));
        ex.Message.Should().Contain("Cannot deduct more than reserved");
    }

    [Fact]
    public void Deduct_MoreThanReserved_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(20, "order-123");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Deduct(30, "order-123"));
        ex.Message.Should().Contain("Cannot deduct more than reserved");
    }

    [Fact]
    public void Deduct_WithZeroQuantity_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(20, "order-123");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => stock.Deduct(0, "order-123"));
        ex.Message.Should().Contain("Quantity must be positive");
    }

    #endregion

    #region Restock Tests

    [Fact]
    public void Restock_WithValidQuantity_EmitsStockRestockedEvent()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(50, "order-1");
        stock.Deduct(50, "order-1");
        var beforeCount = stock.UncommittedEvents.Count;

        // Act
        stock.Restock(50);

        // Assert
        stock.UncommittedEvents.Count.Should().Be(beforeCount + 1);
        var @event = stock.UncommittedEvents.Last().Should().BeOfType<StockRestocked>().Subject;
        @event.ProductId.Should().Be(ProductId);
        @event.Quantity.Should().Be(50);
    }

    [Fact]
    public void Restock_BeforeInitialize_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Restock(10));
        ex.Message.Should().Contain("Stock not initialized");
    }

    [Fact]
    public void Restock_WithZeroQuantity_Throws()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 50);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => stock.Restock(0));
        ex.Message.Should().Contain("Quantity must be positive");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void MultipleReservationsAndPartialDeductions_MaintainsCorrectState()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(30, "order-1");
        stock.Reserve(25, "order-2");

        // Act - Deduct first order
        stock.Deduct(30, "order-1");

        // Assert - Should still have reserved stock for order-2, and available stock
        // Available: 100 - 30 - 25 = 45
        var ex = Assert.Throws<InvalidOperationException>(() => stock.Reserve(46, "order-3"));
        ex.Message.Should().Contain("Insufficient stock");

        // Should be able to reserve the exact available amount
        stock.Reserve(45, "order-3");
    }

    [Fact]
    public void ReserveAfterDeduction_WorksCorrectly()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(50, "order-1");
        stock.Deduct(50, "order-1");

        // Act - After deduction: QuantityOnHand = 50, QuantityReserved = 0, Available = 50
        stock.Reserve(50, "order-2");

        // Assert
        stock.QuantityOnHand.Should().Be(50);  // Deduction reduced this
        stock.QuantityReserved.Should().Be(50); // New reservation
        stock.AvailableQuantity.Should().Be(0);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Initialize_SetsCorrectProperties()
    {
        // Arrange
        var stock = new ProductStockAggregate();

        // Act
        stock.Initialize(ProductId, ProductName, 100);

        // Assert
        stock.Id.Should().Be(ProductId);
        stock.ProductName.Should().Be(ProductName);
        stock.QuantityOnHand.Should().Be(100);
        stock.QuantityReserved.Should().Be(0);
        stock.AvailableQuantity.Should().Be(100);
    }

    [Fact]
    public void Reserve_UpdatesQuantityReserved()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);

        // Act
        stock.Reserve(30, "order-1");

        // Assert
        stock.QuantityOnHand.Should().Be(100);
        stock.QuantityReserved.Should().Be(30);
        stock.AvailableQuantity.Should().Be(70);
    }

    [Fact]
    public void Deduct_UpdatesBothQuantities()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(30, "order-1");

        // Act
        stock.Deduct(30, "order-1");

        // Assert
        stock.QuantityOnHand.Should().Be(70);
        stock.QuantityReserved.Should().Be(0);
        stock.AvailableQuantity.Should().Be(70);
    }

    [Fact]
    public void Restock_IncreasesQuantityOnHand()
    {
        // Arrange
        var stock = new ProductStockAggregate();
        stock.Initialize(ProductId, ProductName, 100);
        stock.Reserve(50, "order-1");
        stock.Deduct(50, "order-1");
        // After deduction: OnHand=50, Reserved=0

        // Act
        stock.Restock(50);

        // Assert
        stock.QuantityOnHand.Should().Be(100);  // 50 + 50
        stock.QuantityReserved.Should().Be(0);
        stock.AvailableQuantity.Should().Be(100);
    }

    #endregion

    #region State Machine Tests

    [Fact]
    public void StateTransitions_FollowExpectedFlow()
    {
        // Arrange & Act
        var stock = new ProductStockAggregate();

        // Assert New state allows Initialize
        Record.Exception(() => stock.Initialize(ProductId, ProductName, 100)).Should().BeNull();
        stock.AvailableQuantity.Should().Be(100);

        // Assert allows multiple reserves
        Record.Exception(() => stock.Reserve(50, "order-1")).Should().BeNull();
        stock.AvailableQuantity.Should().Be(50);

        Record.Exception(() => stock.Reserve(30, "order-2")).Should().BeNull();
        stock.AvailableQuantity.Should().Be(20);

        // Assert allows deduction
        Record.Exception(() => stock.Deduct(50, "order-1")).Should().BeNull();
        stock.QuantityOnHand.Should().Be(50);

        // Assert still allows further operations
        Record.Exception(() => stock.Reserve(20, "order-3")).Should().BeNull();
        stock.AvailableQuantity.Should().Be(0);
    }

    #endregion
}
