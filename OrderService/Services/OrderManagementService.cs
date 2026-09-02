using OrderService.Clients;
using OrderService.DTOs;
using OrderService.Entities;
using OrderService.Repositories;
using Shared.Constants;
using Shared.DTOs;
using Shared.Exceptions;

namespace OrderService.Services;

public class OrderManagementService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryServiceClient _inventoryClient;
    private readonly ILogger<OrderManagementService> _logger;

    public OrderManagementService(
        IOrderRepository orderRepository,
        IInventoryServiceClient inventoryClient,
        ILogger<OrderManagementService> logger)
    {
        _orderRepository = orderRepository;
        _inventoryClient = inventoryClient;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
        {
            throw new ValidationAppException("An order must contain at least one item.");
        }

        // Consolidate duplicate product lines so the same product is never
        // reduced twice independently within one order.
        var consolidatedItems = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
            .ToList();

        // ---- Step 1: check + deduct stock for every line item ----
        // Each call to Inventory Service is atomic for its own product, but
        // across multiple products this is a saga: if any item fails partway
        // through, everything already reduced is rolled back via
        // compensating restore_stock calls, and the whole order is rejected.
        var reservedSoFar = new List<(Guid ProductId, int Quantity)>();

        foreach (var item in consolidatedItems)
        {
            var result = await _inventoryClient.ReduceStockAsync(item.ProductId, item.Quantity);

            switch (result.Outcome)
            {
                case StockAdjustmentOutcome.ProductNotFound:
                    _logger.LogWarning("Order rejected for user {UserId}: product {ProductId} not found", userId, item.ProductId);
                    await CompensateAsync(reservedSoFar);
                    throw new NotFoundAppException($"Product with id '{item.ProductId}' was not found.");

                case StockAdjustmentOutcome.InsufficientStock:
                    _logger.LogWarning(
                        "Order rejected for user {UserId}: insufficient stock for product {ProductId}. Requested {Requested}, available {Available}",
                        userId, item.ProductId, item.Quantity, result.AvailableStock);
                    await CompensateAsync(reservedSoFar);
                    throw new ConflictAppException(
                        $"Insufficient stock for product '{item.ProductId}'. Requested: {item.Quantity}, Available: {result.AvailableStock}.");

                case StockAdjustmentOutcome.Success:
                default:
                    reservedSoFar.Add((item.ProductId, item.Quantity));
                    break;
            }
        }

        // ---- Step 2: create order + items, mark CONFIRMED ----
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            OrderStatus = OrderStatuses.Confirmed,
            CreatedAt = DateTime.UtcNow,
            Items = consolidatedItems.Select(i => new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        try
        {
            var created = await _orderRepository.AddAsync(order);
            _logger.LogInformation(
                "Created order {OrderId} for user {UserId} with {ItemCount} line item(s)",
                created.OrderId, userId, created.Items.Count);

            return Map(created);
        }
        catch (Exception ex)
        {
            // Stock was already deducted on Inventory Service; since order_db
            // failed to persist, compensate by restoring everything reserved
            // so the two services don't drift out of sync.
            _logger.LogError(ex,
                "Failed to persist order for user {UserId} after stock was reserved. Restoring reserved stock.",
                userId);
            await CompensateAsync(reservedSoFar);
            throw;
        }
    }

    public async Task<OrderResponse> GetByIdAsync(Guid orderId, Guid requestingUserId, bool isAdmin)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId);
        if (order == null)
        {
            throw new NotFoundAppException($"Order with id '{orderId}' was not found.");
        }

        if (!isAdmin && order.UserId != requestingUserId)
        {
            throw new ForbiddenAppException("You do not have access to this order.");
        }

        return Map(order);
    }

    public async Task<PagedResult<OrderResponse>> GetMyOrdersAsync(Guid userId, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var (items, totalCount) = await _orderRepository.GetPagedForUserAsync(userId, page, pageSize);

        return new PagedResult<OrderResponse>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OrderResponse> CancelOrderAsync(Guid orderId, Guid requestingUserId, bool isAdmin)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId);
        if (order == null)
        {
            throw new NotFoundAppException($"Order with id '{orderId}' was not found.");
        }

        if (!isAdmin && order.UserId != requestingUserId)
        {
            throw new ForbiddenAppException("You do not have access to this order.");
        }

        if (order.OrderStatus == OrderStatuses.Cancelled)
        {
            throw new ConflictAppException("Order is already cancelled.");
        }

        // Restore stock for every line item. Best-effort per item - if one
        // restore call fails it's logged as critical (see InventoryServiceClient)
        // but the order is still marked cancelled so the user isn't blocked;
        // the failed restore would need manual reconciliation.
        foreach (var item in order.Items)
        {
            await _inventoryClient.RestoreStockAsync(item.ProductId, item.Quantity);
        }

        await _orderRepository.UpdateStatusAsync(orderId, OrderStatuses.Cancelled);
        order.OrderStatus = OrderStatuses.Cancelled;

        _logger.LogInformation("Order {OrderId} cancelled by user {RequestingUserId} (admin: {IsAdmin})", orderId, requestingUserId, isAdmin);

        return Map(order);
    }

    private async Task CompensateAsync(List<(Guid ProductId, int Quantity)> reservedItems)
    {
        foreach (var item in reservedItems)
        {
            await _inventoryClient.RestoreStockAsync(item.ProductId, item.Quantity);
        }
    }

    private static OrderResponse Map(Order order) => new()
    {
        OrderId = order.OrderId,
        UserId = order.UserId,
        OrderStatus = order.OrderStatus,
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList()
    };
}
