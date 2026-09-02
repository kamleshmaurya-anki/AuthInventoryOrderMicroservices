using OrderService.Entities;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order order);
    Task<Order?> GetByIdWithItemsAsync(Guid orderId);
    Task<(List<Order> Items, int TotalCount)> GetPagedForUserAsync(Guid userId, int page, int pageSize);
    Task UpdateStatusAsync(Guid orderId, string newStatus);
}
