using Microsoft.AspNetCore.Mvc;
using StripePayment.Application;
using StripePayment.Infrastructure;

namespace StripePayment.Api.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
public sealed class StripeWebhooksController(
    StripeEvents events, ApplyPayment useCase, ILogger<StripeWebhooksController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        if (!events.Configured) return StatusCode(StatusCodes.Status503ServiceUnavailable);

        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync(ct);
        if (!events.TryRead(raw, Request.Headers["Stripe-Signature"].ToString(), out var payment))
        {
            logger.LogWarning("Rejected an invalid Stripe webhook.");
            return BadRequest();
        }

        return payment is null || await useCase.Apply(payment, ct)
            ? Ok() : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
