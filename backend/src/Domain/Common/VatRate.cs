namespace Domain.Common;

/// <summary>
/// A VAT rate. Deliberately a value rather than a constant: the South African standard rate
/// has changed before and will again, and a historical order must keep the rate that applied
/// when it was placed.
/// </summary>
public readonly record struct VatRate
{
    private VatRate(decimal fraction) => Fraction = fraction;

    /// <summary>The rate as a fraction, e.g. 0.15m for 15%.</summary>
    public decimal Fraction { get; }

    /// <summary>The South African standard rate at the time of writing.</summary>
    public static VatRate Standard { get; } = new(0.15m);

    /// <summary>Zero-rated or exempt supplies.</summary>
    public static VatRate Zero { get; } = new(0m);

    public static VatRate FromFraction(decimal fraction)
    {
        if (fraction is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fraction), fraction, "A VAT rate must be a fraction between 0 and 1.");
        }

        return new VatRate(fraction);
    }

    public static VatRate FromPercent(decimal percent) => FromFraction(percent / 100m);

    /// <summary>The multiplier taking an exclusive amount to an inclusive one.</summary>
    public decimal InclusiveMultiplier => 1m + Fraction;

    public override string ToString() => $"{Fraction * 100m:0.##}%";
}
