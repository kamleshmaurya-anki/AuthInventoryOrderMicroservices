using System.Net;
using System.Net.Http.Json;
using OrderService.DTOs;
using Shared.Exceptions;

namespace OrderService.Clients;

public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductInfoDto?> GetProductAsync(Guid productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/products/{productId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductInfoDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Inventory Service while fetching product {ProductId}", productId);
            throw new ServiceUnavailableAppException($"Inventory Service is unavailable: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timed out calling Inventory Service for product {ProductId}", productId);
            throw new ServiceUnavailableAppException("Inventory Service request timed out.");
        }
    }

    // Calls Inventory Service's atomic reduce_stock endpoint, authenticated
    // with the shared internal API key (not the end user's JWT) - see the
    // handler on the Inventory Service side for why they're kept separate.
    public async Task<StockAdjustmentResult> ReduceStockAsync(Guid productId, int quantity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/products/{productId}/reduce_stock",
                new { Quantity = quantity });

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return StockAdjustmentResult.NotFound();
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var body = await response.Content.ReadFromJsonAsync<InventoryErrorBody>();
                return StockAdjustmentResult.Insufficient(body?.AvailableStock ?? 0);
            }

            response.EnsureSuccessStatusCode();
            return StockAdjustmentResult.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Inventory Service while reducing stock for product {ProductId}", productId);
            throw new ServiceUnavailableAppException($"Inventory Service is unavailable: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timed out calling Inventory Service to reduce stock for product {ProductId}", productId);
            throw new ServiceUnavailableAppException("Inventory Service request timed out.");
        }
    }

    // Best-effort compensating call. Failures are logged as critical but not
    // thrown, since the caller is already unwinding an error path.
    public async Task RestoreStockAsync(Guid productId, int quantity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/products/{productId}/restore_stock",
                new { Quantity = quantity });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogCritical(
                    "Failed to restore {Quantity} units for product {ProductId}. Manual reconciliation may be required. Status: {Status}",
                    quantity, productId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "Exception restoring {Quantity} units for product {ProductId}. Manual reconciliation may be required.",
                quantity, productId);
        }
    }

    private class InventoryErrorBody
    {
        public int AvailableStock { get; set; }
        public string? Message { get; set; }
    }
}
