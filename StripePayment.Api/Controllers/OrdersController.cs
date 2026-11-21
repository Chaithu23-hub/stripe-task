using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using StripePayment.Application;

namespace StripePayment.Api.Controllers;

[ApiController]
[Route("api/orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(IStore store, IDataProtectionProvider protection) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> List(CancellationToken ct)
    {
        var guest = Guest.GetOrCreate(HttpContext, protection);
        return Ok((await store.Orders(guest, ct)).Select(OrderResponse.From));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken ct)
        => await store.Order(id, Guest.Read(HttpContext, protection), ct) is { } order
            ? Ok(OrderResponse.From(order)) : NotFound();

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<OrderStatusResponse>> GetStatus(Guid id, CancellationToken ct)
        => await store.Status(id, Guest.Read(HttpContext, protection), ct) is { } status
            ? Ok(new OrderStatusResponse(status)) : NotFound();

    public sealed record OrderStatusResponse(string Status);
}
