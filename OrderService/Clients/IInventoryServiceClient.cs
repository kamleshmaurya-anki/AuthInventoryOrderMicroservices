using OrderService.DTOs;

namespace OrderService.Clients;

public enum StockAdjustmentOutcome
{
    Success,
    ProductNotFound,
    InsufficientStock
}

public class StockAdjustmentResult
{
    public StockAdjustmentOutcome Outcome { get; init; }
    public int AvailableStock { get; init; }

    public static StockAdjustmentResult Success() => new() { Outcome = StockAdjustmentOutcome.Success };
    public static StockAdjustmentResult NotFound() => new() { Outcome = StockAdjustmentOutcome.ProductNotFound };
    public static StockAdjustmentResult Insufficient(int available) =>
        new() { Outcome = StockAdjustmentOutcome.InsufficientStock, AvailableStock = available };
}

// Abstraction over Inventory Service's HTTP API. This is the ONLY way Order
// Service is allowed to interact with product/stock data - it must never
// open a connection to inventory_db directly.
public interface IInventoryServiceClient
{
    Task<ProductInfoDto?> GetProductAsync(Guid productId);
    Task<StockAdjustmentResult> ReduceStockAsync(Guid productId, int quantity);
    Task RestoreStockAsync(Guid productId, int quantity);
}
