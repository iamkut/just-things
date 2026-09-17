namespace Domain.Ordering;

/// <summary>Order lifecycle. Persisted as the string name.</summary>
public enum OrderStatus
{
    AwaitingPayment,
    Paid,
    InProduction,
    Dispatched,
    Delivered,
    Cancelled,
}
