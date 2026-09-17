using Domain.Common;
using Domain.Sellers;

namespace Domain.Ordering;

/// <summary>A line in a basket. Carries its colour configuration and its resolved price.</summary>
public class CartLine : BaseEntity
{
    public Guid CartId { get; set; }

    public Guid OfferId { get; set; }

    public Offer? Offer { get; set; }

    public int Quantity { get; set; } = 1;

    public LineConfiguration Configuration { get; set; } = new();

    public DateTimeOffset AddedAt { get; set; }

    public decimal LineTotalIncVat => Configuration.ResolvedPriceIncVat * Quantity;
}
