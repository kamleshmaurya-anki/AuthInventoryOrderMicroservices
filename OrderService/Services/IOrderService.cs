using OrderService.DTOs;
using Shared.DTOs;

namespace OrderService.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request);
    Task<OrderResponse> GetByIdAsync(Guid orderId, Guid requestingUserId, bool isAdmin);
    Task<PagedResult<OrderResponse>> GetMyOrdersAsync(Guid userId, int page, int pageSize);
    Task<OrderResponse> CancelOrderAsync(Guid orderId, Guid requestingUserId, bool isAdmin);
}
