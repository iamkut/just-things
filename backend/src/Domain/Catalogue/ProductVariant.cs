using Domain.Common;

namespace Domain.Catalogue;

/// <summary>
/// A physically stocked variant: sheen and pack size ONLY. Colour is deliberately absent --
/// including it would take one topcoat range from about 12 rows to roughly 2,160. See ADR-0002.
/// </summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    public string Sku { get; set; } = string.Empty;

    public Sheen Sheen { get; set; }

    /// <summary>Pack volume in litres. 0.25m for a sample pot.</summary>
    public decimal PackLitres { get; set; }

    /// <summary>Gross weight. Courier rates derive from this; paint is never flat-rated.</summary>
    public decimal WeightKg { get; set; }

    public int? LengthMm { get; set; }

    public int? WidthMm { get; set; }

    public int? HeightMm { get; set; }

    public HazardClass HazardClass { get; set; } = HazardClass.None;

    /// <summary>
    /// Price adjustment for this finish, as a fraction of the offer price -- 0.04m is +4%.
    /// Gloss and eggshell cost more to make than matt.
    /// </summary>
    public decimal SheenUpliftFactor { get; set; }

    public bool IsActive { get; set; } = true;
}
