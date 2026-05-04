using System;
using System.Collections.Generic;
using System.Text;

namespace OrderSystem.Domain
{
    public class OrderItem
    {
        public Guid ProductId { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }

        public OrderItem(Guid productId, decimal unitPrice, int quantity)
        {
            if(productId == Guid.Empty)
            {
                throw new ArgumentException("產品ID不能為空");
            }
            if (unitPrice <= 0)
            {
                throw new ArgumentException("單價必須大於0");
            }
                
            if (quantity <= 0)
            {
                throw new ArgumentException("數量必須大於0");
            }
                
            ProductId = productId;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public decimal Subtotal => UnitPrice * Quantity;   
    }
}