using Api.Contracts;
using Application.Paints;
using Domain.Catalogue;
using Domain.Common;
using Domain.Ordering;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Formatting;

namespace Api.Endpoints;

public static class PaintEndpoints
{
    public static RouteGroupBuilder MapPaintEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Paints");

        group.MapGet("/colours", GetColours)
            .WithSummary("Browse a colour system");

        group.MapGet("/products/{slug}/pricing", GetPricing)
            .WithSummary("Resolve prices for a product against a chosen colour");

        group.MapPost("/products/{slug}/coverage", PostCoverage)
            .WithSummary("Turn an area into the cheapest basket");

        return group;
    }

    private static async Task<IResult> GetColours(
        JustThingsDbContext db,
        string? system,
        string? q,
        decimal? minLrv,
        CancellationToken ct)
    {
        var query = db.Colours.AsNoTracking().Where(c => !c.IsDiscontinued);

        if (!string.IsNullOrWhiteSpace(system))
        {
            query = query.Where(c => c.ColourSystem!.Slug == system);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{term}%") || EF.Functions.ILike(c.Code, $"%{term}%"));
        }

        if (minLrv is not null)
        {
            query = query.Where(c => c.Lrv >= minLrv);
        }

        var colours = await query
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Code)
            .Select(c => new ColourResponse(
                c.Id, c.Code, c.Name, c.Hex, c.Lrv, c.HueFamily.ToString()))
            .ToListAsync(ct);

        return Results.Ok(colours);
    }

    private static async Task<IResult> GetPricing(
        JustThingsDbContext db,
        string slug,
        string? colour,
        CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

        if (product is null)
        {
            return Results.NotFound();
        }

        var variantIds = product.Variants.Select(v => v.Id).ToArray();
        var offers = await db.Offers.AsNoTracking()
            .Where(o => variantIds.Contains(o.ProductVariantId) && o.IsActive)
            .ToListAsync(ct);

        var variantOffers = product.Variants
            .Join(offers, v => v.Id, o => o.ProductVariantId, (v, o) => new VariantOffer(v, o))
            .ToArray();

        Domain.Colours.ColourAvailability? availability = null;
        Domain.Colours.TintBase? tintBase = null;
        Domain.Colours.Colour? chosen = null;

        if (!string.IsNullOrWhiteSpace(colour))
        {
            availability = await db.ColourAvailabilities.AsNoTracking()
                .Include(a => a.Colour)
                .Include(a => a.TintBase)
                .FirstOrDefaultAsync(
                    a => a.ProductId == product.Id
                         && a.Colour!.Code == colour
                         && !a.IsDiscontinued, ct);

            // A colour this product cannot be tinted to has no price. Saying so beats
            // silently pricing it as factory white.
            if (availability is null)
            {
                return Results.NotFound(new { message = $"{product.Name} is not tintable to {colour}." });
            }

            tintBase = availability.TintBase;
            chosen = availability.Colour;
        }

        var pricing = PaintPricingService.PriceProduct(
            product, variantOffers, availability, tintBase, tierMultiplier: 1m, VatRate.Standard);

        var returnability = ReturnabilityPolicy.For(pricing.IsMadeToOrder);

        return Results.Ok(new ProductPricingResponse(
            product.Id,
            product.Name,
            product.CoveragePerLitre,
            product.CoatsRecommended,
            chosen?.Code,
            chosen?.Name,
            chosen?.Hex,
            pricing.TintBaseName,
            pricing.IsMadeToOrder,
            returnability.ToString(),
            pricing.IsMadeToOrder
                ? "Mixed to order, so the seven-day cooling-off right does not apply. Still returnable if it does not match what was ordered."
                : "Returnable unopened within seven days.",
            [.. pricing.Variants.Select(ToResponse)]));
    }

    private static async Task<IResult> PostCoverage(
        JustThingsDbContext db,
        string slug,
        CoverageRequest request,
        CancellationToken ct)
    {
        if (request.AreaSquareMetres <= 0m)
        {
            return Results.BadRequest(new { message = "Area must be greater than zero." });
        }

        if (request.Coats is < 1 or > 5)
        {
            return Results.BadRequest(new { message = "Coats must be between one and five." });
        }

        if (!Enum.TryParse<Sheen>(request.Sheen, ignoreCase: true, out var sheen))
        {
            return Results.BadRequest(new { message = $"Unknown finish '{request.Sheen}'." });
        }

        var product = await db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

        if (product is null)
        {
            return Results.NotFound();
        }

        var variantIds = product.Variants.Select(v => v.Id).ToArray();
        var offers = await db.Offers.AsNoTracking()
            .Where(o => variantIds.Contains(o.ProductVariantId) && o.IsActive)
            .ToListAsync(ct);

        var variantOffers = product.Variants
            .Join(offers, v => v.Id, o => o.ProductVariantId, (v, o) => new VariantOffer(v, o))
            .ToArray();

        Domain.Colours.ColourAvailability? availability = null;
        Domain.Colours.TintBase? tintBase = null;

        if (!string.IsNullOrWhiteSpace(request.ColourCode))
        {
            availability = await db.ColourAvailabilities.AsNoTracking()
                .Include(a => a.TintBase)
                .FirstOrDefaultAsync(
                    a => a.ProductId == product.Id
                         && a.Colour!.Code == request.ColourCode
                         && !a.IsDiscontinued, ct);

            if (availability is null)
            {
                return Results.NotFound(new
                {
                    message = $"{product.Name} is not tintable to {request.ColourCode}.",
                });
            }

            tintBase = availability.TintBase;
        }

        var pricing = PaintPricingService.PriceProduct(
            product, variantOffers, availability, tintBase, tierMultiplier: 1m, VatRate.Standard);

        var plan = PaintPricingService.PlanCoverage(
            product, pricing, sheen, request.AreaSquareMetres, request.Coats);

        if (plan is null)
        {
            return Results.NotFound(new
            {
                message = $"No {sheen} pack sizes are available for {product.Name}.",
            });
        }

        return Results.Ok(new CoverageResponse(
            plan.LitresRequired,
            plan.LitresSupplied,
            plan.SurplusLitres,
            plan.TotalIncVat,
            Zar.Format(plan.TotalIncVat),
            [.. plan.Selections.Select(s => new CoverageLineResponse(
                s.Pack.ProductVariantId,
                s.Pack.Litres,
                s.Quantity,
                s.Pack.UnitPriceIncVat,
                s.LineTotalIncVat))]));
    }

    private static PricedVariantResponse ToResponse(PricedVariant v) =>
        new(v.ProductVariantId,
            v.Sku,
            v.Sheen.ToString(),
            v.PackLitres,
            v.WeightKg,
            v.InStock,
            new PriceBreakdownResponse(
                v.Price.BasePriceExVat,
                v.Price.SheenUpliftExVat,
                v.Price.TintUpliftExVat,
                v.Price.ColourantSurchargeExVat,
                v.Price.TierAdjustmentExVat,
                v.Price.SubtotalExVat,
                v.Price.VatAmount,
                v.Price.TotalIncVat,
                Zar.Format(v.Price.TotalIncVat)));
}
