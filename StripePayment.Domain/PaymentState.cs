namespace StripePayment.Domain;

public static class PaymentState
{
    public static string Next(string current, string eventType, string? paymentStatus)
    {
        if (current == "Paid") return current;
        if (paymentStatus == "paid" && eventType is ("checkout.session.completed" or "checkout.session.async_payment_succeeded"))
            return "Paid";
        if (current != "Pending") return current;
        return eventType switch
        {
            "checkout.session.expired" => "Expired",
            "checkout.session.async_payment_failed" => "Failed",
            _ => current
        };
    }
}
