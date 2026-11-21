using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using Stripe.Checkout;
using StripePayment.Application;
using StripePayment.Infrastructure;

public sealed class StoreTests : IAsyncLifetime
{
    private const string SigningSecret = "whsec_local_fixture_only";
    private readonly string database = "StripePaymentTests_" + Guid.NewGuid().ToString("N");
    private readonly StripeStub stripe = new();
    private WebApplicationFactory<Program> app = null!;

    public async Task InitializeAsync()
    {
        app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Store"] = $@"Server=(localdb)\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true",
                    ["Stripe:WebhookSecret"] = SigningSecret
                }));
            builder.ConfigureServices(services => services.AddSingleton<IPaymentGateway, TestGateway>(
                provider => new TestGateway(provider.GetRequiredService<IConfiguration>(), stripe)));
        });
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<StoreDb>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using (var scope = app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<StoreDb>().Database.EnsureDeletedAsync();
        await app.DisposeAsync();
    }

    [Fact]
    public async Task Checkout_uses_server_prices_deduplicates_requests_and_scopes_orders()
    {
        using var buyer = app.CreateClient();
        using var other = app.CreateClient();
        var cart = new
        {
            requestId = Guid.NewGuid(), customer = Buyer,
            items = new[] { new { productId = 1, quantity = 2, price = 0.01 }, new { productId = 2, quantity = 1, price = 0.01 } },
            total = 0.01
        };

        var first = await buyer.PostAsJsonAsync("/api/checkout/session", cart);
        first.EnsureSuccessStatusCode();
        var checkout = (await first.Content.ReadFromJsonAsync<CheckoutResponse>())!;
        var duplicate = await buyer.PostAsJsonAsync("/api/checkout/session", cart);
        duplicate.EnsureSuccessStatusCode();
        Assert.Equal(checkout, await duplicate.Content.ReadFromJsonAsync<CheckoutResponse>());
        Assert.Equal(1, stripe.Creates);
        Assert.Contains("5000000", stripe.CheckoutBody);
        Assert.Contains("2000000", stripe.CheckoutBody);
        Assert.Equal(120000m, (await buyer.GetFromJsonAsync<OrderResponse>($"/api/orders/{checkout.Id}"))!.Total);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/orders/{checkout.Id}")).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<OrderResponse[]>("/api/orders"))!);
        var crossGuestReplay = await other.PostAsJsonAsync("/api/checkout/session", cart);
        Assert.Equal(HttpStatusCode.Conflict, crossGuestReplay.StatusCode);
        Assert.Equal(1, stripe.Creates);
    }

    [Fact]
    public async Task Checkout_rejects_empty_invalid_and_unavailable_items()
    {
        using var buyer = app.CreateClient();
        foreach (var items in new CheckoutLine[][] { [], [new(1, 11)], [new(999, 1)], [new(1, 1), new(1, 2)] })
        {
            var response = await buyer.PostAsJsonAsync("/api/checkout/session", new CheckoutRequest(Guid.NewGuid(), Buyer, items));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        var invalidCustomer = Buyer with { Email = "invalid", PostalCode = "000000" };
        var invalid = await buyer.PostAsJsonAsync("/api/checkout/session",
            new CheckoutRequest(Guid.NewGuid(), invalidCustomer, [new(1, 1)]));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(0, stripe.Creates);
    }

    [Fact]
    public async Task Webhook_validates_signature_and_total_and_keeps_paid_state_on_replays()
    {
        using var buyer = app.CreateClient();
        var response = await buyer.PostAsJsonAsync("/api/checkout/session", new CheckoutRequest(Guid.NewGuid(), Buyer, [new(1, 1)]));
        response.EnsureSuccessStatusCode();
        var checkout = (await response.Content.ReadFromJsonAsync<CheckoutResponse>())!;
        var paid = Event(checkout.Id, "evt_paid", "checkout.session.completed", "paid", 5000000);

        Assert.Equal(HttpStatusCode.BadRequest, (await SendEvent(buyer, paid, "wrong_secret")).StatusCode);
        (await SendEvent(buyer, Event(checkout.Id, "evt_wrong_amount", "checkout.session.completed", "paid", 1))).EnsureSuccessStatusCode();
        (await SendEvent(buyer, Event(checkout.Id, "evt_unpaid", "checkout.session.completed", "unpaid", 5000000))).EnsureSuccessStatusCode();
        Assert.Equal("Pending", (await buyer.GetFromJsonAsync<OrderResponse>($"/api/orders/{checkout.Id}"))!.Status);

        var deliveries = await Task.WhenAll(SendEvent(buyer, paid), SendEvent(buyer, paid));
        Assert.All(deliveries, delivery => delivery.EnsureSuccessStatusCode());
        (await SendEvent(buyer, Event(checkout.Id, "evt_old_expiry", "checkout.session.expired", "unpaid", 5000000))).EnsureSuccessStatusCode();
        Assert.Equal("Paid", (await buyer.GetFromJsonAsync<OrderResponse>($"/api/orders/{checkout.Id}"))!.Status);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreDb>();
        Assert.Equal(1, await db.ProcessedEvents.CountAsync(e => e.Id == "evt_paid"));
        Assert.Equal("pi_fixture", (await db.Payments.SingleAsync()).PaymentIntentId);
    }

    private static readonly StripePayment.Domain.Customer Buyer = new("Test Buyer", "buyer@example.com", "9876543210",
        "10 Main Street", "Bengaluru", "Karnataka", "560001");

    private static string Event(Guid orderId, string eventId, string type, string status, long amount) =>
        JsonSerializer.Serialize(new
        {
            id = eventId, @object = "event", type, livemode = false,
            data = new
            {
                @object = new
                {
                    id = "cs_test_fixture", @object = "checkout.session", client_reference_id = orderId,
                    amount_total = amount, currency = "inr", payment_status = status, payment_intent = "pi_fixture"
                }
            }
        });

    private static async Task<HttpResponseMessage> SendEvent(HttpClient client, string payload, string secret = SigningSecret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe");
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }

    private sealed class TestGateway(IConfiguration config, StripeStub handler) : StripeGateway(config)
    {
        public override bool Available => true;
        protected override SessionService Sessions() =>
            new(new StripeClient("sk_test_fixture_only", httpClient: new SystemNetHttpClient(new HttpClient(handler, false))));
    }

    private sealed class StripeStub : HttpMessageHandler
    {
        public int Creates { get; private set; }
        public string CheckoutBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Post)
            {
                Creates++;
                Assert.StartsWith("checkout-", Assert.Single(request.Headers.GetValues("Idempotency-Key")));
                CheckoutBody = await request.Content!.ReadAsStringAsync(ct);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"id":"cs_test_fixture","object":"checkout.session","status":"open","url":"https://checkout.stripe.com/c/pay/fixture"}
                    """, Encoding.UTF8, "application/json")
            };
        }
    }
}
