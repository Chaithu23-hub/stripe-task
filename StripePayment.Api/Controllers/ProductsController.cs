using Microsoft.AspNetCore.Mvc;
using StripePayment.Application;
using StripePayment.Domain;

namespace StripePayment.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> List(
        [FromQuery] string? query, [FromQuery] string? category, [FromQuery] string? sort, CancellationToken ct)
        => Ok(await store.Products(query, category, sort, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> Get(int id, CancellationToken ct)
        => await store.Product(id, ct) is { } product ? Ok(product) : NotFound();
}
