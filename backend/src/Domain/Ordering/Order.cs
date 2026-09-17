using Domain.Common;

namespace Domain.Ordering;

/// <summary>A placed order. Shipments split per seller, from day one.</summary>
public class Order : BaseEntity
{
    public Guid VerticalId { get; set; }

    public Guid? CustomerId { get; set; }

    /// <summary>Human-friendly reference quoted on every downstream artifact.</summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.AwaitingPayment;

    public DateTimeOffset PlacedAt { get; set; }

    /// <summary>The VAT rate that applied when this order was placed.</summary>
    public decimal VatFraction { get; set; }

    public decimal SubtotalIncVat { get; set; }

    public decimal ShippingIncVat { get; set; }

    public decimal TotalIncVat { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = [];

    /// <summary>True when any line is mixed to order, which changes the fulfilment path.</summary>
    public bool HasMadeToOrderLines => Lines.Any(l => l.Configuration.IsMadeToOrder);
}
