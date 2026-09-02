namespace OrderService.DTOs;

// Shape returned by Inventory Service's GET /api/products/{id}.
// Order Service never queries inventory_db directly.
public class ProductInfoDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StockQty { get; set; }
    public bool IsActive { get; set; }
}
