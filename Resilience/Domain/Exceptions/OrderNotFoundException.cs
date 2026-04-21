// Domain/Exceptions/OrderExceptions.cs
namespace Resilience.Domain.Exceptions;

public sealed class OrderNotFoundException : Exception
{
    public OrderNotFoundException(string orderId)
        : base($"Order '{orderId}' was not found.") { }
}

public sealed class OrderAlreadyShippedException : Exception
{
    public OrderAlreadyShippedException(string orderId)
        : base($"Order '{orderId}' has already been shipped.") { }
}

public sealed class OrderPaymentAlreadySucceededException : Exception
{
    public OrderPaymentAlreadySucceededException(string orderId)
        : base($"Order '{orderId}' payment has already succeeded.") { }
}