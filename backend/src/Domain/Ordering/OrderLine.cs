using Domain.Common;

namespace Domain.Ordering;

/// <summary>
/// A line on a placed order. Everything needed to fulfil, invoice and argue about it is
/// captured here at placement time.
/// </summary>
public class OrderLine : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid OfferId { get; set; }

    public Guid SellerId { get; set; }

    /// <summary>Snapshotted so the invoice reads correctly after the catalogue moves on.</summary>
    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string SheenName { get; set; } = string.Empty;

    public decimal PackLitres { get; set; }

    public decimal WeightKg { get; set; }

    public int Quantity { get; set; } = 1;

    public LineConfiguration Configuration { get; set; } = new();

    /// <summary>Fixed at placement. Never recomputed from current rules. See ADR-0002.</summary>
    public Returnability Returnability { get; set; }

    public decimal LineTotalIncVat => Configuration.ResolvedPriceIncVat * Quantity;
}
