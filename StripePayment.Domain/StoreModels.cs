using System.ComponentModel.DataAnnotations;

namespace StripePayment.Domain;

public sealed record Product(int Id, string Name, string Description, string Category, decimal Price, string ImageUrl);
public sealed record OrderItem(int ProductId, string Name, decimal UnitPrice, int Quantity)
{
    public int Id { get; set; }
}

public sealed class Order
{
    public Guid Id { get; init; }
    public Guid GuestId { get; init; }
    public Guid RequestId { get; init; }
    public string RequestHash { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public Customer Customer { get; init; } = null!;
    public decimal Total { get; init; }
    public List<OrderItem> Items { get; init; } = [];
    public PaymentTransaction Payment { get; init; } = new();
}

public sealed class PaymentTransaction
{
    public Guid OrderId { get; init; }
    public string? SessionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string Status { get; set; } = "Pending";

}

public sealed record ProcessedWebhookEvent(string Id, DateTimeOffset ReceivedAt);

public sealed record Customer
{
    public Customer() { }

    public Customer(string name, string email, string phone, string address, string city, string state, string postalCode)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Address = address;
        City = city;
        State = state;
        PostalCode = postalCode;
    }

    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = "";

    [Required, RegularExpression(@"^\+?[0-9 ()-]{7,20}$")]
    public string Phone { get; init; } = "";

    [Required, StringLength(250)]
    public string Address { get; init; } = "";

    [Required, StringLength(100)]
    public string City { get; init; } = "";

    [Required, StringLength(100)]
    public string State { get; init; } = "";

    [Required, RegularExpression(@"^[1-9][0-9]{5}$")]
    public string PostalCode { get; init; } = "";
}

public static class Money
{
    public const string Currency = "inr";
    public static long ToPaise(decimal value)
    {
        if (value <= 0 || decimal.Round(value, 2) != value)
            throw new ArgumentOutOfRangeException(nameof(value));
        return checked((long)(value * 100));
    }
}
