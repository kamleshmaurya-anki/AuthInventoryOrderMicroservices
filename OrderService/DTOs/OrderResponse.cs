namespace OrderService.DTOs;

public class OrderItemResponse
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderResponse
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
}
