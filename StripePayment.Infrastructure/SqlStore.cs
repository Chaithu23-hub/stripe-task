using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StripePayment.Application;
using StripePayment.Domain;

namespace StripePayment.Infrastructure;

public sealed class SqlStore(StoreDb db) : IStore
{
    public async Task<IReadOnlyList<Product>> Products(string? query, string? category, string? sort, CancellationToken ct)
    {
        var products = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query)) products = products.Where(p => p.Name.Contains(query.Trim()));
        if (!string.IsNullOrWhiteSpace(category)) products = products.Where(p => p.Category == category);
        products = sort switch
        {
            "asc" => products.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "desc" => products.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            _ => products.OrderBy(p => p.Id)
        };
        return await products.ToListAsync(ct);
    }

    public async Task<Product?> Product(int id, CancellationToken ct) => await db.Products.FindAsync([id], ct);
    public async Task<IReadOnlyDictionary<int, Product>> Products(int[] ids, CancellationToken ct) =>
        await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

    private IQueryable<Order> FullOrders => db.Orders.Include(o => o.Payment).Include(o => o.Items);

    public async Task<IReadOnlyList<Order>> Orders(Guid guest, CancellationToken ct) =>
        await FullOrders.AsNoTracking().Where(o => o.GuestId == guest).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
    public Task<Order?> Order(Guid id, Guid? guest, CancellationToken ct) =>
        FullOrders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && o.GuestId == guest, ct);
    public Task<string?> Status(Guid id, Guid? guest, CancellationToken ct) =>
        db.Orders.Where(o => o.Id == id && o.GuestId == guest).Select(o => o.Payment.Status).SingleOrDefaultAsync(ct);
    public Task<Order?> FindAttempt(Guid guest, Guid requestId, CancellationToken ct) =>
        FullOrders.SingleOrDefaultAsync(o => o.GuestId == guest && o.RequestId == requestId, ct);
    public Task<Order?> FindPaymentOrder(Guid id, CancellationToken ct) =>
        db.Orders.Include(o => o.Payment).SingleOrDefaultAsync(o => o.Id == id, ct);
    public Task<bool> HasEvent(string id, CancellationToken ct) => db.ProcessedEvents.AnyAsync(e => e.Id == id, ct);
    public void AddOrder(Order order) => db.Orders.Add(order);
    public void AddEvent(ProcessedWebhookEvent stripeEvent) => db.ProcessedEvents.Add(stripeEvent);

    public async Task<bool> TrySave(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { return false; }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { return false; }
    }

    public void Reset() => db.ChangeTracker.Clear();
}
