using Domain.Catalogue;
using Domain.Common;

namespace Domain.Sellers;

/// <summary>
/// A seller offering a variant at a price. First-class from day one even with one seller:
/// retrofitting it later means rewriting pricing, cart, checkout, order lines, shipment
/// splitting and reporting simultaneously, while live. See ADR-0004.
/// </summary>
public class Offer : BaseEntity
{
    public Guid SellerId { get; set; }

    public Seller? Seller { get; set; }

    public Guid ProductVariantId { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    /// <summary>
    /// The white-base price for this pack, exclusive of VAT. Tint uplift is added at
    /// resolution time -- it is never baked into this figure.
    /// </summary>
    public decimal PriceExVat { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>Working days from order to dispatch, before any tint-to-dispatch allowance.</summary>
    public int LeadTimeDays { get; set; }

    public FulfilmentMode FulfilmentMode { get; set; } = FulfilmentMode.SellerDropship;

    public bool IsActive { get; set; } = true;
}
