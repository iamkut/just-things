using Application.Paints;
using Domain.Catalogue;
using Domain.Colours;
using Domain.Common;
using Domain.Sellers;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Paints;

public class PaintPricingServiceTests
{
    private static readonly Product Architect = new()
    {
        Id = Guid.NewGuid(),
        Name = "Architect Premium Interior",
        CoveragePerLitre = 8m,
        CoatsRecommended = 2,
        IsTintable = true,
    };

    private static readonly TintBase Pastel = new()
    {
        Id = Guid.NewGuid(), Name = "Pastel base", UpliftPerLitreExVat = 6m,
    };

    private static readonly TintBase Deep = new()
    {
        Id = Guid.NewGuid(), Name = "Deep base", UpliftPerLitreExVat = 26m,
    };

    private static VariantOffer Pack(decimal litres, decimal priceExVat, Sheen sheen = Sheen.Matt, int stock = 10)
    {
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = Architect.Id,
            Sku = $"ARCH-{sheen}-{litres}L",
            Sheen = sheen,
            PackLitres = litres,
            WeightKg = litres * 1.3m,
            SheenUpliftFactor = sheen == Sheen.Matt ? 0m : 0.06m,
        };

        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variant.Id,
            PriceExVat = priceExVat,
            StockQuantity = stock,
        };

        return new VariantOffer(variant, offer);
    }

    private static readonly VariantOffer[] MattPacks =
    [
        Pack(1m, 189.00m),
        Pack(5m, 749.00m),
        Pack(20m, 2609.00m),
    ];

    private static ColourAvailability Availability(TintBase tintBase, decimal surcharge = 0m) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = Architect.Id,
        ColourId = Guid.NewGuid(),
        TintBaseId = tintBase.Id,
        ColourantSurchargeExVat = surcharge,
    };

    [Fact]
    public void Factory_white_prices_without_any_tint_uplift()
    {
        var pricing = PaintPricingService.PriceProduct(
            Architect, MattPacks, availability: null, tintBase: null, 1m, VatRate.Standard);

        pricing.IsMadeToOrder.Should().BeFalse();
        pricing.TintBaseName.Should().BeNull();
        pricing.Variants.Should().HaveCount(3);
        pricing.Variants.Single(v => v.PackLitres == 5m).Price.TotalIncVat.Should().Be(861.35m);
    }

    [Fact]
    public void Choosing_a_colour_makes_the_line_made_to_order_and_changes_the_price()
    {
        var pricing = PaintPricingService.PriceProduct(
            Architect, MattPacks, Availability(Pastel), Pastel, 1m, VatRate.Standard);

        pricing.IsMadeToOrder.Should().BeTrue();
        pricing.TintBaseName.Should().Be("Pastel base");
        pricing.Variants.Single(v => v.PackLitres == 5m).Price.TotalIncVat.Should().Be(895.85m);
    }

    [Fact]
    public void A_deeper_base_costs_more_on_the_same_pack()
    {
        var pastel = PaintPricingService.PriceProduct(
            Architect, MattPacks, Availability(Pastel), Pastel, 1m, VatRate.Standard);
        var deep = PaintPricingService.PriceProduct(
            Architect, MattPacks, Availability(Deep), Deep, 1m, VatRate.Standard);

        var pastelFive = pastel.Variants.Single(v => v.PackLitres == 5m).Price.TotalIncVat;
        var deepFive = deep.Variants.Single(v => v.PackLitres == 5m).Price.TotalIncVat;

        deepFive.Should().BeGreaterThan(pastelFive);
        (deepFive - pastelFive).Should().Be(115.00m); // (26 - 6) x 5L x 1.15
    }

    [Fact]
    public void Tint_uplift_scales_with_pack_size()
    {
        var pricing = PaintPricingService.PriceProduct(
            Architect, MattPacks, Availability(Deep), Deep, 1m, VatRate.Standard);

        pricing.Variants.Single(v => v.PackLitres == 1m).Price.TintUpliftExVat.Should().Be(26m);
        pricing.Variants.Single(v => v.PackLitres == 5m).Price.TintUpliftExVat.Should().Be(130m);
        pricing.Variants.Single(v => v.PackLitres == 20m).Price.TintUpliftExVat.Should().Be(520m);
    }

    [Fact]
    public void Inactive_variants_and_offers_are_left_out()
    {
        var retired = Pack(10m, 1500m);
        retired.Variant.IsActive = false;

        var pricing = PaintPricingService.PriceProduct(
            Architect, [.. MattPacks, retired], null, null, 1m, VatRate.Standard);

        pricing.Variants.Should().HaveCount(3);
        pricing.Variants.Should().NotContain(v => v.PackLitres == 10m);
    }

    [Fact]
    public void Out_of_stock_is_reported_but_still_priced()
    {
        var pricing = PaintPricingService.PriceProduct(
            Architect, [Pack(5m, 749m, stock: 0)], null, null, 1m, VatRate.Standard);

        var variant = pricing.Variants.Single();
        variant.InStock.Should().BeFalse();
        variant.Price.TotalIncVat.Should().Be(861.35m);
    }

    [Fact]
    public void A_tinted_line_without_its_base_is_rejected()
    {
        var act = () => PaintPricingService.PriceProduct(
            Architect, MattPacks, Availability(Pastel), tintBase: null, 1m, VatRate.Standard);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tintBase");
    }

    [Fact]
    public void Coverage_plan_uses_the_prices_resolved_for_the_chosen_colour()
    {
        var pricing = PaintPricingService.PriceProduct(
            Architect, MattPacks, Availability(Pastel), Pastel, 1m, VatRate.Standard);

        // 60 m2, two coats, 8 m2/L => 15 litres.
        var plan = PaintPricingService.PlanCoverage(Architect, pricing, Sheen.Matt, 60m, 2)!;

        plan.LitresRequired.Should().Be(15m);
        plan.Selections.Should().ContainSingle();
        plan.Selections[0].Quantity.Should().Be(3);
        plan.Selections[0].Pack.Litres.Should().Be(5m);
        plan.TotalIncVat.Should().Be(2687.55m); // 3 x 895.85
        plan.SurplusLitres.Should().Be(0m);
    }

    [Fact]
    public void Coverage_plan_does_not_mix_finishes()
    {
        var mixed = new[]
        {
            Pack(5m, 749.00m, Sheen.Matt),
            Pack(1m, 10.00m, Sheen.Gloss), // absurdly cheap, and must still be ignored
        };

        var pricing = PaintPricingService.PriceProduct(
            Architect, mixed, null, null, 1m, VatRate.Standard);

        var plan = PaintPricingService.PlanCoverage(Architect, pricing, Sheen.Matt, 40m, 2)!;

        plan.Selections.Should().OnlyContain(s => s.Pack.Litres == 5m);
    }

    [Fact]
    public void Trade_tier_lowers_every_variant()
    {
        var retail = PaintPricingService.PriceProduct(
            Architect, MattPacks, null, null, 1m, VatRate.Standard);
        var trade = PaintPricingService.PriceProduct(
            Architect, MattPacks, null, null, 0.85m, VatRate.Standard);

        trade.Variants.Should().HaveCount(retail.Variants.Count);
        for (var i = 0; i < retail.Variants.Count; i++)
        {
            trade.Variants[i].Price.TotalIncVat
                .Should().BeLessThan(retail.Variants[i].Price.TotalIncVat);
        }
    }
}
