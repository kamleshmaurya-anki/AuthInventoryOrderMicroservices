using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

public class CreateProductRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string ProductName { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "StockQty must be >= 0")]
    public int StockQty { get; set; }
}
