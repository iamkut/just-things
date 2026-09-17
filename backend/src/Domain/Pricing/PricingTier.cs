using Domain.Common;

namespace Domain.Pricing;

/// <summary>
/// A customer price tier. Retail is the implicit 1.0 tier; trade tiers arrive in Phase 2.
/// </summary>
public class PricingTier : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Applied to the resolved exclusive amount. 1.0m is retail, 0.85m is 15% off trade.
    /// Must be greater than zero.
    /// </summary>
    public decimal Multiplier { get; set; } = 1m;

    public bool IsDefault { get; set; }
}
