using Microsoft.EntityFrameworkCore;
using InventoryService.Data;
using InventoryService.Entities;

namespace InventoryService.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _context;

    public ProductRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Product> AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public Task<Product?> GetByIdAsync(Guid productId)
    {
        return _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Products.AsNoTracking().OrderBy(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        var existing = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == product.ProductId);
        if (existing == null)
        {
            return false;
        }

        existing.ProductName = product.ProductName;
        existing.StockQty = product.StockQty;
        existing.IsActive = product.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid productId)
    {
        var existing = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (existing == null)
        {
            return false;
        }

        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // Atomic check-and-deduct in a single UPDATE, so concurrent order
    // requests can never both succeed against the same last units.
    public async Task<StockAdjustmentResult> ReduceStockAsync(Guid productId, int quantity)
    {
        var product = await _context.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null || !product.IsActive)
        {
            return StockAdjustmentResult.NotFound();
        }

        var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE products
            SET stock_qty = stock_qty - {quantity}, updated_at = GETUTCDATE()
            WHERE product_id = {productId} AND stock_qty >= {quantity} AND is_active = 1");

        if (rowsAffected == 0)
        {
            var latest = await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);
            return StockAdjustmentResult.InsufficientStock(latest?.StockQty ?? 0);
        }

        return StockAdjustmentResult.Ok();
    }

    // Compensating action - adds units back (order failed after stock was
    // deducted, or an order was cancelled).
    public async Task<bool> RestoreStockAsync(Guid productId, int quantity)
    {
        var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE products
            SET stock_qty = stock_qty + {quantity}, updated_at = GETUTCDATE()
            WHERE product_id = {productId}");

        return rowsAffected > 0;
    }
}
