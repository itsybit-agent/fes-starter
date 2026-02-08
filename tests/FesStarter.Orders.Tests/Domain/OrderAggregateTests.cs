using FesStarter.Events.Orders;
using FesStarter.Orders;

namespace FesStarter.Orders.Tests.Domain;

public class OrderAggregateTests
{
    private const string OrderId = "order-123";
    private const string ProductId = "product-456";

    #region Place Order Tests

    [Fact]
    public void Place_WithValidQuantity_EmitsOrderPlacedEvent()
    {
        // Arrange
        var order = new OrderAggregate();
        var quantity = 5;

        // Act
        order.Place(OrderId, ProductId, quantity);

        // Assert
        order.UncommittedEvents.Should().HaveCount(1);
        var @event = order.UncommittedEvents.First().Should().BeOfType<OrderPlaced>().Subject;
        @event.OrderId.Should().Be(OrderId);
        @event.ProductId.Should().Be(ProductId);
        @event.Quantity.Should().Be(quantity);
    }

    [Fact]
    public void Place_WithZeroQuantity_Throws()
    {
        // Arrange
        var order = new OrderAggregate();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => order.Place(OrderId, ProductId, 0));
        ex.Message.Should().Contain("Quantity must be positive");
    }

    [Fact]
    public void Place_WithNegativeQuantity_Throws()
    {
        // Arrange
        var order = new OrderAggregate();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => order.Place(OrderId, ProductId, -1));
        ex.Message.Should().Contain("Quantity must be positive");
    }

    [Fact]
    public void Place_TwiceOnSameAggregate_Throws()
    {
        // Arrange
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => order.Place("order-456", ProductId, 3));
        ex.Message.Should().Contain("Order already exists");
    }

    #endregion

    #region Mark Reserved Tests

    [Fact]
    public void MarkReserved_AfterPlace_TransitionsToReservedState()
    {
        // Arrange
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);
        var beforeCount = order.UncommittedEvents.Count;

        // Act
        order.MarkReserved();

        // Assert
        order.UncommittedEvents.Count.Should().Be(beforeCount + 1);
        var @event = order.UncommittedEvents.Last().Should().BeOfType<OrderStockReserved>().Subject;
        @event.OrderId.Should().Be(OrderId);
    }

    [Fact]
    public void MarkReserved_WhenShipped_Throws()
    {
        // Arrange
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);
        order.MarkReserved();
        order.Ship();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => order.MarkReserved());
        ex.Message.Should().Contain("Cannot reserve order in status");
    }

    [Fact]
    public void MarkReserved_TwiceOnSameAggregate_Throws()
    {
        // Arrange
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);
        order.MarkReserved();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => order.MarkReserved());
        ex.Message.Should().Contain("Cannot reserve order in status");
    }

    #endregion

    #region Ship Order Tests

    [Fact]
    public void Ship_AfterReservation_EmitsOrderShippedEvent()
    {
        // Arrange
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);
        order.MarkReserved();
        var beforeCount = order.UncommittedEvents.Count;

        // Act
        order.Ship();

        // Assert
        order.UncommittedEvents.Count.Should().Be(beforeCount + 1);
        var @event = order.UncommittedEvents.Last().Should().BeOfType<OrderShipped>().Subject;
        @event.OrderId.Should().Be(OrderId);
        @event.ShippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Ship_BeforePlace_Throws()
    {
        // Arrange
        var order = new OrderAggregate();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => order.Ship());
        ex.Message.Should().Contain("Cannot ship order in status");
    }

    [Fact]
    public void Ship_BeforeReservation_Throws()
    {
        // Arrange
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => order.Ship());
        ex.Message.Should().Contain("Cannot ship order in status");
    }

    [Fact]
    public void Ship_TwiceOnSameAggregate_Throws()
    {
        // Arrange
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);
        order.MarkReserved();
        order.Ship();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => order.Ship());
        ex.Message.Should().Contain("Cannot ship order in status");
    }

    #endregion

    #region State Machine Tests

    [Fact]
    public void StateTransitions_FollowExpectedFlow()
    {
        // Arrange & Act
        var order = new OrderAggregate();
        order.Status.Should().Be(OrderStatus.Pending);

        // Act: Place order
        order.Place(OrderId, ProductId, 5);
        order.Status.Should().Be(OrderStatus.Pending);

        // Act: Reserve order
        order.MarkReserved();
        order.Status.Should().Be(OrderStatus.Placed);

        // Act: Ship order
        order.Ship();
        order.Status.Should().Be(OrderStatus.Shipped);

        // Assert: No further state changes allowed
        Assert.Throws<InvalidOperationException>(() => order.Ship());
        Assert.Throws<InvalidOperationException>(() => order.MarkReserved());
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Place_SetsCorrectProperties()
    {
        // Arrange
        var order = new OrderAggregate();
        var now = DateTime.UtcNow;

        // Act
        order.Place(OrderId, ProductId, 42);

        // Assert
        order.Id.Should().Be(OrderId);
        order.ProductId.Should().Be(ProductId);
        order.Quantity.Should().Be(42);
        order.CreatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        order.ShippedAt.Should().BeNull();
    }

    [Fact]
    public void Ship_SetsShippedAtTimestamp()
    {
        // Arrange
        var order = new OrderAggregate();
        var now = DateTime.UtcNow;
        order.Place(OrderId, ProductId, 5);
        order.MarkReserved();

        // Act
        order.Ship();

        // Assert
        order.ShippedAt.Should().NotBeNull();
        order.ShippedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Event Sequence Tests

    [Fact]
    public void CompleteOrderFlow_GeneratesCorrectEventSequence()
    {
        // Arrange & Act
        var order = new OrderAggregate();
        order.Place(OrderId, ProductId, 5);
        order.MarkReserved();
        order.Ship();

        // Assert
        order.UncommittedEvents.Should().HaveCount(3);

        var events = order.UncommittedEvents.ToList();
        events[0].Should().BeOfType<OrderPlaced>();
        events[1].Should().BeOfType<OrderStockReserved>();
        events[2].Should().BeOfType<OrderShipped>();

        var placed = (OrderPlaced)events[0];
        placed.OrderId.Should().Be(OrderId);
        placed.ProductId.Should().Be(ProductId);
        placed.Quantity.Should().Be(5);
    }

    #endregion
}
