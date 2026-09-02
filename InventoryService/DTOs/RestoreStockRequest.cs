using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

// Compensating action: called by Order Service when a reduce_stock reservation
// must be rolled back (order save failed, or order was cancelled).
public class RestoreStockRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}
