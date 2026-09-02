using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;
using Shared.DTOs;
using Shared.Security;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize] // every endpoint here requires a valid JWT
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // POST api/orders
    // Any authenticated user (User or Admin) can place an order for
    // themselves. Validates stock via Inventory Service before confirming.
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var userId = User.GetUserId();
        var created = await _orderService.CreateOrderAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.OrderId }, created);
    }

    // GET api/orders/my-orders?page=&pageSize=
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(PagedResult<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();
        var result = await _orderService.GetMyOrdersAsync(userId, page, pageSize);
        return Ok(result);
    }

    // GET api/orders/{id}
    // Users can only view their own orders; Admins can view any order.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var order = await _orderService.GetByIdAsync(id, userId, isAdmin);
        return Ok(order);
    }

    // PATCH api/orders/{id}/cancel
    // Restores stock and marks the order CANCELLED. Users can cancel their
    // own orders; Admins can cancel any order.
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var cancelled = await _orderService.CancelOrderAsync(id, userId, isAdmin);
        return Ok(cancelled);
    }
}
