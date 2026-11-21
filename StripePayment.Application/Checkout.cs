using System.Security.Cryptography;
using System.Text.Json;
using StripePayment.Domain;

namespace StripePayment.Application;

public sealed class Checkout(IStore store, IPaymentGateway gateway)
{
    public async Task<CheckoutOutcome> Create(Guid guest, CheckoutRequest request, CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty || request.Items is not { Length: >= 1 and <= 20 } ||
            request.Items.Any(line => line is null || line.Quantity is < 1 or > 10 || line.ProductId <= 0) ||
            request.Items.Select(line => line.ProductId).Distinct().Count() != request.Items.Length)
            return new(null, "Use a valid request ID and one line per product with a quantity from 1 to 10.", 400);
        if (!gateway.Available) return new(null, "Stripe test mode is not configured.", 503);

        var items = request.Items.OrderBy(i => i.ProductId).ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { request.Customer, items })));
        var order = await store.FindAttempt(guest, request.RequestId, ct);
        if (order is null)
        {
            var ids = items.Select(i => i.ProductId).ToArray();
            var products = await store.Products(ids, ct);
            if (products.Count != ids.Length) return new(null, "A product in your cart is unavailable.", 400);

            var snapshots = items.Select(i => new OrderItem(i.ProductId, products[i.ProductId].Name,
                products[i.ProductId].Price, i.Quantity)).ToList();
            var total = snapshots.Sum(i => i.UnitPrice * i.Quantity);
            if (total < 0.50m || total > 9999999.99m)
                return new(null, "The order total is outside Stripe's INR limits.", 400);

            order = new Order
            {
                Id = Guid.NewGuid(), GuestId = guest, RequestId = request.RequestId, RequestHash = hash,
                CreatedAt = DateTimeOffset.UtcNow, Customer = request.Customer, Items = snapshots, Total = total
            };
            store.AddOrder(order);
            if (!await store.TrySave(ct))
            {
                store.Reset();
                order = await store.FindAttempt(guest, request.RequestId, ct);
                if (order is null) return new(null, "This checkout reference could not be used in this browser.", 409);
            }
        }

        if (order.RequestHash != hash) return new(null, "This checkout reference belongs to a different cart or customer.", 409);
        if (order.Payment.Status != "Pending")
            return new(null, "This attempt is closed. Review it in Orders or submit again to start a new order.", 409, true);
        if (order.Payment.SessionId is null && DateTimeOffset.UtcNow - order.CreatedAt > TimeSpan.FromHours(23))
            return new(null, "This attempt is too old to retry. Check your orders.", 409);

        var session = order.Payment.SessionId is { } existing
            ? await gateway.Get(existing, ct) : await gateway.Create(order, ct);
        if (order.Payment.SessionId is null)
        {
            order.Payment.SessionId = session.Id;
            if (!await store.TrySave(ct))
            {
                store.Reset();
                var saved = await store.FindPaymentOrder(order.Id, ct);
                if (saved?.Payment.SessionId != session.Id)
                    return new(null, "Checkout could not be finalized. Try again.", 503);
            }
        }
        return session.Status == "open" && session.Url is { Length: > 0 } url
            ? new(new CheckoutResponse(order.Id, url))
            : new(null, "This Stripe session is closed. Check its status in Orders.", 409);
    }
}
