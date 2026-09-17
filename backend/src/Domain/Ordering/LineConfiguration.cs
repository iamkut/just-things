namespace Domain.Ordering;

/// <summary>
/// The paint-specific payload on a cart or order line: which colour, which base, and what the
/// customer was actually shown.
/// </summary>
/// <remarks>
/// Every field here is a SNAPSHOT, not a lookup. Colours get renamed, withdrawn and repriced.
/// An order placed in March must still print correctly in September, so the line carries its
/// own copy rather than joining back to a record that has since moved. This is not redundancy.
/// </remarks>
public class LineConfiguration
{
    /// <summary>The colour chosen, or null for factory white.</summary>
    public Guid? ColourId { get; set; }

    public string? ColourCode { get; set; }

    public string? ColourName { get; set; }

    public string? ColourHex { get; set; }

    public Guid? TintBaseId { get; set; }

    public string? TintBaseName { get; set; }

    /// <summary>True whenever a tint is applied. Drives returnability and the dispensing docket.</summary>
    public bool IsMadeToOrder { get; set; }

    /// <summary>The resolved exclusive amount the customer was shown.</summary>
    public decimal ResolvedPriceExVat { get; set; }

    /// <summary>The resolved inclusive amount the customer was shown, and will be charged.</summary>
    public decimal ResolvedPriceIncVat { get; set; }

    /// <summary>The VAT rate that applied when this line was priced.</summary>
    public decimal VatFraction { get; set; }

    /// <summary>A factory-white configuration.</summary>
    public static LineConfiguration Untinted(decimal priceExVat, decimal priceIncVat, decimal vatFraction) =>
        new()
        {
            IsMadeToOrder = false,
            ResolvedPriceExVat = priceExVat,
            ResolvedPriceIncVat = priceIncVat,
            VatFraction = vatFraction,
        };
}
