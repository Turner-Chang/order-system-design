namespace OrderSystem.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public decimal Totalamount { get; private set; }
    public OrderStatus Status { get; private set; }
    
    private Order() {}

    public static Order Create(decimal totalamount)
    {
        if (totalamount <= 0)
            throw new ArgumentException("訂單金額必須大於0");

        return new Order
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Totalamount = totalamount,
            Status = OrderStatus.Pending
        };
    }
}