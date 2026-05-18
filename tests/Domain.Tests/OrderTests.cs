using FluentAssertions;

namespace OrderSystem.Domain.Tests;

public class OrderTests
{
    [Fact]
    // 意圖：驗證新訂單一出生是空的——Items.Count == 0、TotalAmount == 0、Status == Pending
    public void Create_ShouldReturnEmptyOrder()
    {
        // Act
        var order = Order.Create();
        
        //Assert
        order.Items.Count.Should().Be(0);
        order.TotalAmount.Should().Be(0);
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    // 意圖：加一個 100×2 的 item，驗證 Items 長度 +1 且 TotalAmount 自動算出 200
    public void AddItem_ShouldAppendToItemsAndUpdateTotal()
    {
        // Arrange
        var order = Order.Create();
        var item = new OrderItem(Guid.NewGuid(), 100m, 2); // Subtotal = 200
        
        // Act
        order.AddItem(item);
        
        // Assert
        order.Items.Count.Should().Be(1);
        order.Items[0].Should().Be(item);
        order.TotalAmount.Should().Be(200m);
    }

    [Fact]
    // 意圖：傳 null 進 AddItem 要拋 ArgumentNullException
    public void AddItem_WithNullItem_ShouldThrow()
    {
        // Arrange
        var order = Order.Create();
        
        // Act
        var act = () => order.AddItem(null!);
        
        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}