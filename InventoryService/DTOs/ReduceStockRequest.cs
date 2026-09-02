using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

// Called internally by Order Service (via internal API key, not user JWT)
// to atomically validate + deduct stock for one order line item.
public class ReduceStockRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}
