using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;

namespace OrderService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    // Order + its items are inserted in a single SaveChangesAsync call,
    // which EF Core wraps in one database transaction - either both the
    // order row and every item row are written, or none are.
    public async Task<Order> AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public Task<Order?> GetByIdWithItemsAsync(Guid orderId)
    {
        return _context.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetPagedForUserAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task UpdateStatusAsync(Guid orderId, string newStatus)
    {
        await _context.Orders
            .Where(o => o.OrderId == orderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.OrderStatus, newStatus));
    }
}
