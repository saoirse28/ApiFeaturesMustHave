namespace APIVersioning.Domain
{
    public enum OrderStatus
    {
        Pending, PaymentConfirmed, Processing,
        Shipped, Delivered, Cancelled, Refunded
    }
}
