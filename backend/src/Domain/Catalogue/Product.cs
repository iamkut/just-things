using Domain.Common;

namespace Domain.Catalogue;

/// <summary>
/// The marketable product, e.g. "Architect Premium Interior". A product does NOT carry a
/// colour: colour is a line-item configuration, not a variant axis. See ADR-0002.
/// </summary>
public class Product : BaseEntity
{
    public Guid VerticalId { get; set; }

    public Guid BrandId { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Square metres covered per litre, on a prepared surface, one coat.</summary>
    public decimal CoveragePerLitre { get; set; }

    /// <summary>Coats the manufacturer recommends. The coverage calculator defaults to this.</summary>
    public int CoatsRecommended { get; set; } = 2;

    /// <summary>False for products that are never tinted -- a solvent, a cleaning sundry.</summary>
    public bool IsTintable { get; set; } = true;

    public ICollection<ProductVariant> Variants { get; set; } = [];
}
