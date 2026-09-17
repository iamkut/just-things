namespace Domain.Common;

/// <summary>
/// Money rules for the platform. Amounts are <see cref="decimal"/> everywhere -- never
/// <c>double</c> or <c>float</c> -- and are stored exclusive of VAT.
/// </summary>
/// <remarks>
/// Rounding happens ONCE, at the end of a calculation, on the VAT-inclusive figure.
/// Rounding intermediate uplifts produces cents-level drift that surfaces as baskets
/// whose lines do not sum to the total. See docs/data-model.md.
/// </remarks>
public static class Money
{
    /// <summary>Currency scale for ZAR.</summary>
    public const int Scale = 2;

    /// <summary>
    /// Rounds to cents, away from zero at the midpoint -- the convention South African
    /// retail invoices are expected to follow.
    /// </summary>
    public static decimal Round(decimal amount) =>
        Math.Round(amount, Scale, MidpointRounding.AwayFromZero);

    /// <summary>Throws when an amount that must not be negative is.</summary>
    public static decimal RequireNonNegative(decimal amount, string paramName)
    {
        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, amount, "Amount may not be negative.");
        }

        return amount;
    }
}
