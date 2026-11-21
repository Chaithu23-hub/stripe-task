using StripePayment.Domain;

namespace StripePayment.Application;

public interface IStore
{
    Task<IReadOnlyList<Product>> Products(string? query, string? category, string? sort, CancellationToken ct);
    Task<Product?> Product(int id, CancellationToken ct);
    Task<IReadOnlyDictionary<int, Product>> Products(int[] ids, CancellationToken ct);
    Task<IReadOnlyList<Order>> Orders(Guid guest, CancellationToken ct);
    Task<Order?> Order(Guid id, Guid? guest, CancellationToken ct);
    Task<Order?> FindAttempt(Guid guest, Guid requestId, CancellationToken ct);
    Task<Order?> FindPaymentOrder(Guid id, CancellationToken ct);
    Task<string?> Status(Guid id, Guid? guest, CancellationToken ct);
    Task<bool> HasEvent(string id, CancellationToken ct);
    void AddOrder(Order order);
    void AddEvent(ProcessedWebhookEvent stripeEvent);
    Task<bool> TrySave(CancellationToken ct);
    void Reset();
}

public interface IPaymentGateway
{
    bool Available { get; }
    Task<CheckoutSession> Create(Order order, CancellationToken ct);
    Task<CheckoutSession> Get(string sessionId, CancellationToken ct);
}

public sealed record CheckoutSession(string Id, string? Status, string? Url);
public sealed record PaymentEvent(string Id, string Type, Guid OrderId, string SessionId,
    string? Currency, long? Amount, string? PaymentStatus, string? PaymentIntentId);
public sealed record CheckoutOutcome(CheckoutResponse? Session, string? Error = null, int ErrorStatus = 0, bool AttemptClosed = false);
