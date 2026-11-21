using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using StripePayment.Application;

namespace StripePayment.Api.Controllers;

[ApiController]
[Route("api/checkout")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CheckoutController(Checkout checkout, IDataProtectionProvider protection) : ControllerBase
{
    [HttpPost("session")]
    public async Task<ActionResult<CheckoutResponse>> CreateSession([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var result = await checkout.Create(Guest.GetOrCreate(HttpContext, protection), request, ct);
        return result.Session is { } session ? Ok(session) :
            Problem(detail: result.Error, statusCode: result.ErrorStatus,
                extensions: result.AttemptClosed ? new Dictionary<string, object?> { ["attemptClosed"] = true } : null);
    }
}
