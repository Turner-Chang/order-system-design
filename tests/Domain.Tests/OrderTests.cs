using FluentAssertions;

namespace OrderSystem.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void Create_WithVakudAmount_ShouldSucceed()
    {
        // Arrange
        decimal amount = 100m;
        
        // Act
        var order = Order.Create(amount);
        
        // Assert
        order.Should().NotBeNull();
        order.Totalamount.Should().Be(amount);
        order.Status.Should().Be(OrderStatus.Pending);
    }
    
    [Fact]
    public void Create_WithZeroAmount_ShouldThrow()
    {
        // Act
        var act = () => Order.Create(0);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}