using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using StripePayment.Application;
using StripePayment.Domain;

namespace StripePayment.Infrastructure;

public class StripeGateway(IConfiguration config) : IPaymentGateway
{
    private string? TestKey => config["Stripe:SecretKey"] is { } key &&
        key.StartsWith("sk_test_", StringComparison.Ordinal) ? key : null;

    public virtual bool Available => TestKey is not null;

    protected virtual SessionService Sessions() => new(new StripeClient(TestKey!));

    public async Task<CheckoutSession> Create(Order order, CancellationToken ct)
    {
        var origin = config["Store:FrontendUrl"]!.TrimEnd('/');
        var session = await Sessions().CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = order.Customer.Email,
            ClientReferenceId = order.Id.ToString(),
            SuccessUrl = $"{origin}/payment/success?orderId={order.Id}",
            CancelUrl = $"{origin}/payment/cancel?orderId={order.Id}",
            LineItems = order.Items.OrderBy(i => i.ProductId).Select(i => new SessionLineItemOptions
            {
                Quantity = i.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = Money.Currency, UnitAmount = Money.ToPaise(i.UnitPrice),
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = i.Name }
                }
            }).ToList()
        }, new RequestOptions { IdempotencyKey = $"checkout-{order.Id:N}" }, ct);
        return new(session.Id, session.Status, session.Url);
    }

    public async Task<CheckoutSession> Get(string sessionId, CancellationToken ct)
    {
        var session = await Sessions().GetAsync(sessionId, cancellationToken: ct);
        return new(session.Id, session.Status, session.Url);
    }
}
