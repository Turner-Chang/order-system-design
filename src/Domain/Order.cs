namespace OrderSystem.Domain;

public class Order
{
    private readonly List<OrderItem> _items =new ();
    public IReadOnlyList<OrderItem> Items => _items;
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public decimal TotalAmount => _items.Sum(i => i.Subtotal);
    public OrderStatus Status { get; private set; }

    
    private Order() {}

    public static Order Create()
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
        };
    }

    public void AddItem(OrderItem item)
    {
     if (item == null) throw new ArgumentNullException(nameof(item));
        _items.Add(item);
    }
}