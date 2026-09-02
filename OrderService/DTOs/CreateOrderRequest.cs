using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class OrderItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    [Required, MinLength(1, ErrorMessage = "An order must contain at least one item.")]
    public List<OrderItemRequest> Items { get; set; } = new();
}
