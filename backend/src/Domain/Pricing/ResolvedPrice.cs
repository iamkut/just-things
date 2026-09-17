namespace Domain.Pricing;

/// <summary>
/// The outcome of a price resolution. <see cref="TotalIncVat"/> is the authoritative figure --
/// it is what the customer is shown and charged, and the only one rounded from the raw
/// calculation. The components are for display and reconciliation.
/// </summary>
public sealed record ResolvedPrice
{
    /// <summary>The white-base pack price before any uplift.</summary>
    public required decimal BasePriceExVat { get; init; }

    /// <summary>Amount added for the finish.</summary>
    public required decimal SheenUpliftExVat { get; init; }

    /// <summary>Amount added for the tint base. Zero on an untinted line.</summary>
    public required decimal TintUpliftExVat { get; init; }

    public required decimal ColourantSurchargeExVat { get; init; }

    /// <summary>Negative for a trade discount, zero at retail.</summary>
    public required decimal TierAdjustmentExVat { get; init; }

    /// <summary>Exclusive subtotal, rounded for display.</summary>
    public required decimal SubtotalExVat { get; init; }

    /// <summary>
    /// Derived as <see cref="TotalIncVat"/> less <see cref="SubtotalExVat"/> rather than
    /// computed independently, so an invoice line and its VAT always sum to the total.
    /// </summary>
    public required decimal VatAmount { get; init; }

    /// <summary>What the customer pays. Rounded once, at the end.</summary>
    public required decimal TotalIncVat { get; init; }
}
