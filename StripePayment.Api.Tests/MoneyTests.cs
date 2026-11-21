using StripePayment.Domain;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("12.34", 1234)]
    [InlineData("50000", 5000000)]
    public void ConvertsINRToPaise(string value, long expected) =>
        Assert.Equal(expected, Money.ToPaise(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void RejectsFractionsOfPaiseAndNonPositivePrices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.ToPaise(0.001m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.ToPaise(0m));
    }

    [Theory]
    [InlineData("Pending", "checkout.session.completed", "unpaid", "Pending")]
    [InlineData("Pending", "checkout.session.async_payment_failed", "unpaid", "Failed")]
    [InlineData("Pending", "checkout.session.expired", "unpaid", "Expired")]
    [InlineData("Failed", "checkout.session.async_payment_succeeded", "paid", "Paid")]
    [InlineData("Paid", "checkout.session.expired", "unpaid", "Paid")]
    public void PaymentStatusIsMonotonic(string current, string type, string stripeStatus, string expected) =>
        Assert.Equal(expected, PaymentState.Next(current, type, stripeStatus));
}
