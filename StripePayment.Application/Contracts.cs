using System.ComponentModel.DataAnnotations;
using StripePayment.Domain;
namespace StripePayment.Application;
public sealed record CheckoutLine
{
    public CheckoutLine() { }

    public CheckoutLine(int productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 10)]
    public int Quantity { get; init; }
}

public sealed record CheckoutRequest
{
    public CheckoutRequest() { }

    public CheckoutRequest(Guid requestId, Customer customer, CheckoutLine[] items)
    {
        RequestId = requestId;
        Customer = customer;
        Items = items;
    }

    public Guid RequestId { get; init; }

    [Required]
    public Customer Customer { get; init; } = null!;

    [Required, MinLength(1), MaxLength(20)]
    public CheckoutLine[] Items { get; init; } = [];
}
public sealed record CheckoutResponse(Guid Id, string Url);
public sealed record OrderLine(string Name, decimal UnitPrice, int Quantity);
public sealed record OrderResponse(Guid Id, string Status, decimal Total, DateTimeOffset CreatedAt, IEnumerable<OrderLine> Items)
{
    public static OrderResponse From(Order order) => new(order.Id, order.Payment.Status, order.Total,
        order.CreatedAt, order.Items.Select(i => new OrderLine(i.Name, i.UnitPrice, i.Quantity)));
}
