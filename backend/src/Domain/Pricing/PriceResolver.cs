using Domain.Common;

namespace Domain.Pricing;

/// <summary>
/// Resolves a line price at runtime. Pure -- no I/O -- so it is unit-testable.
/// </summary>
/// <remarks>
/// Price is never read off a row. Because colour is a line-item configuration rather than a
/// variant axis (ADR-0002), the tint base is only known once a colour is chosen, so the price
/// has to be computed from:
/// <code>
/// offer price + sheen uplift + (base uplift per litre x pack litres) + colourant surcharge
/// </code>
/// then adjusted for the customer tier, with VAT applied last and rounding done exactly once.
/// </remarks>
public static class PriceResolver
{
    public static ResolvedPrice Resolve(in PriceRequest request)
    {
        var basePrice = request.OfferPriceExVat;
        var sheenUplift = basePrice * request.SheenUpliftFactor;
        var tintUplift = request.TintBaseUpliftPerLitreExVat * request.PackLitres;

        var beforeTier = basePrice + sheenUplift + tintUplift + request.ColourantSurchargeExVat;
        var afterTier = beforeTier * request.TierMultiplier;
        var tierAdjustment = afterTier - beforeTier;

        // Round once, on the inclusive figure. Rounding each uplift first produces
        // cents-level drift that shows up as baskets whose lines do not sum to the total.
        var totalIncVat = Money.Round(afterTier * request.Vat.InclusiveMultiplier);
        var subtotalExVat = Money.Round(afterTier);

        return new ResolvedPrice
        {
            BasePriceExVat = Money.Round(basePrice),
            SheenUpliftExVat = Money.Round(sheenUplift),
            TintUpliftExVat = Money.Round(tintUplift),
            ColourantSurchargeExVat = Money.Round(request.ColourantSurchargeExVat),
            TierAdjustmentExVat = Money.Round(tierAdjustment),
            SubtotalExVat = subtotalExVat,
            VatAmount = totalIncVat - subtotalExVat,
            TotalIncVat = totalIncVat,
        };
    }
}
