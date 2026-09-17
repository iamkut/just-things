using Domain.Catalogue;
using Domain.Colours;
using Domain.Common;
using Domain.Coverage;
using Domain.Pricing;
using Domain.Sellers;

namespace Application.Paints;

/// <summary>A stocked variant together with the offer selling it.</summary>
public readonly record struct VariantOffer(ProductVariant Variant, Offer Offer);

/// <summary>One variant priced for the colour the customer has chosen.</summary>
public sealed record PricedVariant
{
    public required Guid ProductVariantId { get; init; }

    public required string Sku { get; init; }

    public required Sheen Sheen { get; init; }

    public required decimal PackLitres { get; init; }

    public required decimal WeightKg { get; init; }

    public required ResolvedPrice Price { get; init; }

    public required bool InStock { get; init; }
}

/// <summary>A product priced against a chosen colour, with everything the page needs.</summary>
public sealed record ProductPricing
{
    public required Guid ProductId { get; init; }

    public required IReadOnlyList<PricedVariant> Variants { get; init; }

    /// <summary>Null when the customer has not chosen a colour, i.e. factory white.</summary>
    public required Guid? ColourId { get; init; }

    public required string? TintBaseName { get; init; }

    /// <summary>True when a colour was chosen, so the tin is mixed to order.</summary>
    public required bool IsMadeToOrder { get; init; }
}

/// <summary>
/// Prices a product against a colour and turns an area into a basket. Pure -- it takes
/// already-loaded entities rather than a repository -- so it is unit-testable without mocks.
/// </summary>
public static class PaintPricingService
{
    /// <summary>
    /// Prices every variant of a product for one colour choice.
    /// </summary>
    /// <param name="availability">
    /// The colour-availability row for the chosen colour, or null for factory white. When a
    /// colour is chosen but no availability row exists, the product cannot be tinted to it and
    /// there is no price to show -- the caller should treat that as not-found, not as white.
    /// </param>
    public static ProductPricing PriceProduct(
        Product product,
        IReadOnlyList<VariantOffer> variantOffers,
        ColourAvailability? availability,
        TintBase? tintBase,
        decimal tierMultiplier,
        VatRate vat)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(variantOffers);

        if (availability is not null && tintBase is null)
        {
            throw new ArgumentNullException(
                nameof(tintBase), "A tinted line needs the tint base its colour resolves to.");
        }

        var priced = variantOffers
            .Where(vo => vo.Variant.IsActive && vo.Offer.IsActive)
            .OrderBy(vo => vo.Variant.PackLitres)
            .Select(vo => new PricedVariant
            {
                ProductVariantId = vo.Variant.Id,
                Sku = vo.Variant.Sku,
                Sheen = vo.Variant.Sheen,
                PackLitres = vo.Variant.PackLitres,
                WeightKg = vo.Variant.WeightKg,
                InStock = vo.Offer.StockQuantity > 0,
                Price = PriceResolver.Resolve(
                    availability is null
                        ? PriceRequest.Untinted(vo.Offer, vo.Variant, tierMultiplier, vat)
                        : PriceRequest.Tinted(
                            vo.Offer, vo.Variant, availability, tintBase!, tierMultiplier, vat)),
            })
            .ToArray();

        return new ProductPricing
        {
            ProductId = product.Id,
            Variants = priced,
            ColourId = availability?.ColourId,
            TintBaseName = tintBase?.Name,
            IsMadeToOrder = availability is not null,
        };
    }

    /// <summary>
    /// Works out how much paint a job needs and the cheapest way to buy it, at the prices
    /// resolved for the chosen colour.
    /// </summary>
    /// <param name="sheen">
    /// Restricts the plan to one finish. A basket that mixed matt and silk to save money would
    /// not paint the same wall.
    /// </param>
    public static CoveragePlan? PlanCoverage(
        Product product,
        ProductPricing pricing,
        Sheen sheen,
        decimal areaSquareMetres,
        int coats)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(pricing);

        var litres = CoverageCalculator.LitresRequired(
            areaSquareMetres, coats, product.CoveragePerLitre);

        var packs = pricing.Variants
            .Where(v => v.Sheen == sheen)
            .Select(v => new PackOption(v.ProductVariantId, v.PackLitres, v.Price.TotalIncVat))
            .ToArray();

        return CoverageCalculator.CheapestPlan(packs, litres);
    }
}
