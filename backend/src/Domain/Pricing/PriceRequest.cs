using Domain.Catalogue;
using Domain.Colours;
using Domain.Common;
using Domain.Sellers;

namespace Domain.Pricing;

/// <summary>
/// Everything the resolver needs to price one line. Primitive by design so the calculation
/// stays pure and testable without an entity graph.
/// </summary>
public readonly record struct PriceRequest
{
    public PriceRequest(
        decimal offerPriceExVat,
        decimal sheenUpliftFactor,
        decimal tintBaseUpliftPerLitreExVat,
        decimal packLitres,
        decimal colourantSurchargeExVat,
        decimal tierMultiplier,
        VatRate vat)
    {
        Money.RequireNonNegative(offerPriceExVat, nameof(offerPriceExVat));
        Money.RequireNonNegative(sheenUpliftFactor, nameof(sheenUpliftFactor));
        Money.RequireNonNegative(tintBaseUpliftPerLitreExVat, nameof(tintBaseUpliftPerLitreExVat));
        Money.RequireNonNegative(colourantSurchargeExVat, nameof(colourantSurchargeExVat));

        if (packLitres <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(packLitres), packLitres, "Pack volume must be greater than zero.");
        }

        if (tierMultiplier <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tierMultiplier), tierMultiplier, "A tier multiplier must be greater than zero.");
        }

        OfferPriceExVat = offerPriceExVat;
        SheenUpliftFactor = sheenUpliftFactor;
        TintBaseUpliftPerLitreExVat = tintBaseUpliftPerLitreExVat;
        PackLitres = packLitres;
        ColourantSurchargeExVat = colourantSurchargeExVat;
        TierMultiplier = tierMultiplier;
        Vat = vat;
    }

    /// <summary>The white-base price for this pack, exclusive of VAT.</summary>
    public decimal OfferPriceExVat { get; }

    /// <summary>Finish adjustment as a fraction of the offer price. 0.04m is +4%.</summary>
    public decimal SheenUpliftFactor { get; }

    /// <summary>Base uplift per litre. Zero when the line is untinted.</summary>
    public decimal TintBaseUpliftPerLitreExVat { get; }

    public decimal PackLitres { get; }

    public decimal ColourantSurchargeExVat { get; }

    public decimal TierMultiplier { get; }

    public VatRate Vat { get; }

    /// <summary>A factory-white line: no colour chosen, so no base uplift and no surcharge.</summary>
    public static PriceRequest Untinted(
        Offer offer, ProductVariant variant, decimal tierMultiplier, VatRate vat) =>
        new(offer.PriceExVat, variant.SheenUpliftFactor, 0m, variant.PackLitres, 0m, tierMultiplier, vat);

    /// <summary>
    /// A tinted line. The availability row supplies the base, which is what makes the price
    /// depend on the chosen colour. See ADR-0002.
    /// </summary>
    public static PriceRequest Tinted(
        Offer offer,
        ProductVariant variant,
        ColourAvailability availability,
        TintBase tintBase,
        decimal tierMultiplier,
        VatRate vat) =>
        new(
            offer.PriceExVat,
            variant.SheenUpliftFactor,
            tintBase.UpliftPerLitreExVat,
            variant.PackLitres,
            availability.ColourantSurchargeExVat,
            tierMultiplier,
            vat);
}
