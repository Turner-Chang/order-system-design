using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace OrderSystem.Domain.Tests
{
    public class OrderItemTests
    {
        [Fact]
        public void Constructor_WithValidArguments_ShouldCreateOrderItem()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var unitPrice = 100m;
            var quantity = 2;

            // Act
            var item = new OrderItem(productId, unitPrice, quantity);

            // Assert
            item.ProductId.Should().Be(productId);
            item.UnitPrice.Should().Be(unitPrice);
            item.Quantity.Should().Be(quantity);
        }
        [Fact]
        public void Subtotal_ShouldBeUnitPriceMultiplyByQuantity()
        {
            var item = new OrderItem(Guid.NewGuid(), 100m, 3);

            item.Subtotal.Should().Be(300m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithNonPositiveQuantity_ShouldThrow(int invalidQuantity)
        {
            var act = () => new OrderItem(Guid.NewGuid(), 100m, invalidQuantity);

            act.Should().Throw<ArgumentException>();
        }
    }
}
