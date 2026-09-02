using Shared.Constants;

namespace OrderService.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string OrderStatus { get; set; } = OrderStatuses.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}
