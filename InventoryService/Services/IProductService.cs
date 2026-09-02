using InventoryService.DTOs;
using Shared.DTOs;

namespace InventoryService.Services;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> GetByIdAsync(Guid productId);
    Task<PagedResult<ProductResponse>> GetPagedAsync(int page, int pageSize);
    Task<ProductResponse> UpdateAsync(Guid productId, UpdateProductRequest request);
    Task DeleteAsync(Guid productId);
    Task<ReduceStockResponse> ReduceStockAsync(Guid productId, ReduceStockRequest request);
    Task RestoreStockAsync(Guid productId, RestoreStockRequest request);
}
