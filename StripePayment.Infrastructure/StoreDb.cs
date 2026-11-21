using Microsoft.EntityFrameworkCore;
using StripePayment.Domain;
namespace StripePayment.Infrastructure;
public sealed class StoreDb(DbContextOptions<StoreDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PaymentTransaction> Payments => Set<PaymentTransaction>();
    public DbSet<ProcessedWebhookEvent> ProcessedEvents => Set<ProcessedWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Product>().Property(p => p.Price).HasPrecision(12, 2);
        model.Entity<OrderItem>().Property(p => p.UnitPrice).HasPrecision(12, 2);
        model.Entity<Order>().Property(o => o.Total).HasPrecision(14, 2);
        model.Entity<Order>().HasIndex(o => o.GuestId);
        model.Entity<Order>().OwnsOne(o => o.Customer);
        model.Entity<Order>().HasIndex(o => o.RequestId).IsUnique();
        model.Entity<Order>().HasOne(o => o.Payment).WithOne().HasForeignKey<PaymentTransaction>(p => p.OrderId);
        model.Entity<PaymentTransaction>().HasKey(p => p.OrderId);
        model.Entity<PaymentTransaction>().Property<byte[]>("Version").IsRequired().IsRowVersion();
        model.Entity<PaymentTransaction>().Property(p => p.SessionId).HasMaxLength(255);
        model.Entity<PaymentTransaction>().HasIndex(p => p.SessionId).IsUnique();
        model.Entity<Product>().HasData(
            new Product(1, "Laptop", "A versatile notebook for work and study.", "Computers", 50000m, "/products/laptop.jpg"),
            new Product(2, "Mobile", "An everyday smartphone with a crisp display.", "Mobile", 20000m, "/products/phone.jpg"),
            new Product(3, "Headphones", "Over-ear headphones made for focused listening.", "Audio", 30000m, "/products/headphones.jpg"));
    }
}
