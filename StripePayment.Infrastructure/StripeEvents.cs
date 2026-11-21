using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using StripePayment.Application;

namespace StripePayment.Infrastructure;

public sealed class StripeEvents(IConfiguration config)
{
    public bool Configured => !string.IsNullOrWhiteSpace(config["Stripe:WebhookSecret"]);

    public bool TryRead(string body, string signature, out PaymentEvent? payment)
    {
        payment = null;
        Event evt;
        try
        {
            evt = EventUtility.ConstructEvent(body, signature, config["Stripe:WebhookSecret"]!,
                throwOnApiVersionMismatch: false);
        }
        catch (Exception ex) when (ex is StripeException or System.Text.Json.JsonException or Newtonsoft.Json.JsonException)
        {
            return false;
        }
        if (!evt.Livemode && evt.Type is ("checkout.session.completed" or "checkout.session.async_payment_succeeded"
            or "checkout.session.async_payment_failed" or "checkout.session.expired") &&
            evt.Data.Object is Session session && Guid.TryParse(session.ClientReferenceId, out var id))
            payment = new(evt.Id, evt.Type, id, session.Id, session.Currency, session.AmountTotal,
                session.PaymentStatus, session.PaymentIntentId);
        return true;
    }
}
