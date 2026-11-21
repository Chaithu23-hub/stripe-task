using StripePayment.Domain;

namespace StripePayment.Application;

public sealed class ApplyPayment(IStore store)
{
    // False tells the transport to retry the delivery.
    public async Task<bool> Apply(PaymentEvent stripeEvent, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await store.HasEvent(stripeEvent.Id, ct)) return true;
            var order = await store.FindPaymentOrder(stripeEvent.OrderId, ct);
            if (order is null) return true;
            var payment = order.Payment;
            if (payment.SessionId is null) return false;
            if (payment.SessionId != stripeEvent.SessionId || stripeEvent.Currency != Money.Currency ||
                stripeEvent.Amount != Money.ToPaise(order.Total)) return true;

            var status = PaymentState.Next(payment.Status, stripeEvent.Type, stripeEvent.PaymentStatus);
            if (status != payment.Status)
            {
                payment.Status = status;
                payment.PaymentIntentId = stripeEvent.PaymentIntentId;
            }
            store.AddEvent(new ProcessedWebhookEvent(stripeEvent.Id, DateTimeOffset.UtcNow));
            if (await store.TrySave(ct)) return true;
            store.Reset();
        }
        return false;
    }
}
