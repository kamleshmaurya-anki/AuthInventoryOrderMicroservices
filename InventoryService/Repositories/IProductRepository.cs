using InventoryService.Entities;

namespace InventoryService.Repositories;

public class StockAdjustmentResult
{
    public bool Success { get; private set; }
    public bool ProductFound { get; private set; }
    public int AvailableStock { get; private set; }

    public static StockAdjustmentResult Ok() => new() { Success = true, ProductFound = true };
    public static StockAdjustmentResult NotFound() => new() { Success = false, ProductFound = false };
    public static StockAdjustmentResult InsufficientStock(int available) =>
        new() { Success = false, ProductFound = true, AvailableStock = available };
}

public interface IProductRepository
{
    Task<Product> AddAsync(Product product);
    Task<Product?> GetByIdAsync(Guid productId);
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    Task<bool> UpdateAsync(Product product);
    Task<bool> SoftDeleteAsync(Guid productId);
    Task<StockAdjustmentResult> ReduceStockAsync(Guid productId, int quantity);
    Task<bool> RestoreStockAsync(Guid productId, int quantity);
}
