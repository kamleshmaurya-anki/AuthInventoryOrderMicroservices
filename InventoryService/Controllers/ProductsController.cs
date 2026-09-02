using InventoryService.DTOs;
using InventoryService.Security;
using InventoryService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.DTOs;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/products")]
[Authorize] // every user-facing endpoint here requires a valid JWT
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    // POST api/products - Admin only
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var created = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
    }

    // GET api/products/{id} - any authenticated user
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        return Ok(product);
    }

    // GET api/products?page=&pageSize= - any authenticated user
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _productService.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    // PUT api/products/{id} - Admin only
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var updated = await _productService.UpdateAsync(id, request);
        return Ok(updated);
    }

    // DELETE api/products/{id} - Admin only (soft delete -> is_active = 0)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }

    // POST api/products/{id}/reduce_stock
    // Internal endpoint used by Order Service during checkout. Secured by a
    // shared internal API key, NOT by end-user JWT/roles - see
    // InternalApiKeyAuthenticationHandler for why.
    [HttpPost("{id:guid}/reduce_stock")]
    [Authorize(AuthenticationSchemes = InternalApiKeyAuthenticationHandler.SchemeName)]
    [ProducesResponseType(typeof(ReduceStockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReduceStock(Guid id, [FromBody] ReduceStockRequest request)
    {
        var result = await _productService.ReduceStockAsync(id, request);
        return Ok(result);
    }

    // POST api/products/{id}/restore_stock
    // Internal endpoint: compensating action for a cancelled order or a
    // reservation that could not be committed on Order Service's side.
    [HttpPost("{id:guid}/restore_stock")]
    [Authorize(AuthenticationSchemes = InternalApiKeyAuthenticationHandler.SchemeName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestoreStock(Guid id, [FromBody] RestoreStockRequest request)
    {
        await _productService.RestoreStockAsync(id, request);
        return NoContent();
    }
}
