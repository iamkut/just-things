using Domain.Common;

namespace Domain.Colours;

/// <summary>
/// The join that makes runtime pricing possible. For a given product and colour it answers
/// two questions at once: can this product be tinted to this colour at all, and which base
/// does it resolve to. Without this row there is no price to show. See docs/data-model.md.
/// </summary>
public class ColourAvailability : BaseEntity
{
    public Guid ProductId { get; set; }

    public Guid ColourId { get; set; }

    public Colour? Colour { get; set; }

    public Guid TintBaseId { get; set; }

    public TintBase? TintBase { get; set; }

    /// <summary>Optional per-combination surcharge, exclusive of VAT. Usually zero.</summary>
    public decimal ColourantSurchargeExVat { get; set; }

    public bool IsDiscontinued { get; set; }
}
