using InventoryService.DTOs;
using InventoryService.Entities;
using InventoryService.Repositories;
using Shared.DTOs;
using Shared.Exceptions;

namespace InventoryService.Services;

public class ProductManagementService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductManagementService> _logger;

    public ProductManagementService(IProductRepository repository, ILogger<ProductManagementService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            ProductName = request.ProductName,
            StockQty = request.StockQty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(product);
        _logger.LogInformation("Created product {ProductId} ({ProductName})", created.ProductId, created.ProductName);

        return Map(created);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid productId)
    {
        var product = await _repository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new NotFoundAppException($"Product with id '{productId}' was not found.");
        }

        return Map(product);
    }

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize);

        return new PagedResult<ProductResponse>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductResponse> UpdateAsync(Guid productId, UpdateProductRequest request)
    {
        var product = new Product
        {
            ProductId = productId,
            ProductName = request.ProductName,
            StockQty = request.StockQty,
            IsActive = request.IsActive
        };

        var updated = await _repository.UpdateAsync(product);
        if (!updated)
        {
            throw new NotFoundAppException($"Product with id '{productId}' was not found.");
        }

        _logger.LogInformation("Updated product {ProductId}", productId);
        return await GetByIdAsync(productId);
    }

    public async Task DeleteAsync(Guid productId)
    {
        var deleted = await _repository.SoftDeleteAsync(productId);
        if (!deleted)
        {
            throw new NotFoundAppException($"Product with id '{productId}' was not found.");
        }

        _logger.LogInformation("Soft-deleted product {ProductId}", productId);
    }

    public async Task<ReduceStockResponse> ReduceStockAsync(Guid productId, ReduceStockRequest request)
    {
        var result = await _repository.ReduceStockAsync(productId, request.Quantity);

        if (!result.ProductFound)
        {
            throw new NotFoundAppException($"Product with id '{productId}' was not found.");
        }

        if (!result.Success)
        {
            _logger.LogWarning(
                "Stock reduction failed for product {ProductId}. Requested: {Requested}, Available: {Available}",
                productId, request.Quantity, result.AvailableStock);
            throw new ConflictAppException(
                $"Insufficient stock for product '{productId}'. Requested: {request.Quantity}, Available: {result.AvailableStock}.");
        }

        _logger.LogInformation("Reduced stock of product {ProductId} by {Quantity}", productId, request.Quantity);

        return new ReduceStockResponse
        {
            Success = true,
            ProductId = productId,
            Message = "Stock reduced successfully."
        };
    }

    public async Task RestoreStockAsync(Guid productId, RestoreStockRequest request)
    {
        var restored = await _repository.RestoreStockAsync(productId, request.Quantity);
        if (!restored)
        {
            throw new NotFoundAppException($"Product with id '{productId}' was not found.");
        }

        _logger.LogInformation("Restored {Quantity} units to product {ProductId}", request.Quantity, productId);
    }

    private static ProductResponse Map(Product product) => new()
    {
        ProductId = product.ProductId,
        ProductName = product.ProductName,
        StockQty = product.StockQty,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
