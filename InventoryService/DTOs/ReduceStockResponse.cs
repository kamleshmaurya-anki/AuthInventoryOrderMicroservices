namespace InventoryService.DTOs;

public class ReduceStockResponse
{
    public bool Success { get; set; }
    public Guid ProductId { get; set; }
    public string Message { get; set; } = string.Empty;
}
