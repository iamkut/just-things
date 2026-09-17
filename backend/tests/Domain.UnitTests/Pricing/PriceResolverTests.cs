using Domain.Common;
using Domain.Pricing;
using FluentAssertions;
using Xunit;

namespace Domain.UnitTests.Pricing;

public class PriceResolverTests
{
    private const decimal ArchitectFiveLitreExVat = 749.00m;

    private static PriceRequest Request(
        decimal offer = ArchitectFiveLitreExVat,
        decimal sheenFactor = 0m,
        decimal baseUpliftPerLitre = 0m,
        decimal litres = 5m,
        decimal surcharge = 0m,
        decimal tier = 1m) =>
        new(offer, sheenFactor, baseUpliftPerLitre, litres, surcharge, tier, VatRate.Standard);

    [Fact]
    public void Untinted_line_is_the_offer_price_plus_vat()
    {
        var price = PriceResolver.Resolve(Request());

        price.TintUpliftExVat.Should().Be(0m);
        price.SubtotalExVat.Should().Be(749.00m);
        price.TotalIncVat.Should().Be(861.35m);
    }

    [Theory]
    // base uplift per litre, expected inclusive total for a 5L pack at R749 ex VAT
    [InlineData(0, 861.35)]    // white
    [InlineData(6, 895.85)]    // pastel
    [InlineData(14, 941.85)]   // medium
    [InlineData(26, 1010.85)]  // deep
    [InlineData(38, 1079.85)]  // accent
    public void Tint_base_drives_the_price(decimal upliftPerLitre, decimal expectedIncVat)
    {
        var price = PriceResolver.Resolve(Request(baseUpliftPerLitre: upliftPerLitre));

        price.TotalIncVat.Should().Be(expectedIncVat);
    }

    [Fact]
    public void Sheen_uplift_is_a_fraction_of_the_offer_price()
    {
        // Eggshell at +6% on R749, on a pastel base.
        var price = PriceResolver.Resolve(Request(sheenFactor: 0.06m, baseUpliftPerLitre: 6m));

        price.SheenUpliftExVat.Should().Be(44.94m);
        price.TintUpliftExVat.Should().Be(30.00m);
        price.SubtotalExVat.Should().Be(823.94m);
        price.TotalIncVat.Should().Be(947.53m);
    }

    [Fact]
    public void Trade_tier_discount_is_reported_as_a_negative_adjustment()
    {
        var price = PriceResolver.Resolve(Request(baseUpliftPerLitre: 6m, tier: 0.85m));

        price.TierAdjustmentExVat.Should().Be(-116.85m);
        price.SubtotalExVat.Should().Be(662.15m);
        price.TotalIncVat.Should().Be(761.47m);
    }

    [Fact]
    public void Colourant_surcharge_is_added_before_vat()
    {
        var price = PriceResolver.Resolve(Request(surcharge: 12.50m));

        price.ColourantSurchargeExVat.Should().Be(12.50m);
        price.SubtotalExVat.Should().Be(761.50m);
        price.TotalIncVat.Should().Be(875.73m);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.99)]
    [InlineData(17.77)]
    [InlineData(749.00)]
    [InlineData(2609.55)]
    [InlineData(99999.99)]
    public void Subtotal_and_vat_always_reconcile_to_the_total(decimal offer)
    {
        var price = PriceResolver.Resolve(Request(offer: offer, sheenFactor: 0.06m, baseUpliftPerLitre: 26m));

        // An invoice line plus its VAT must equal what the customer is charged, always.
        (price.SubtotalExVat + price.VatAmount).Should().Be(price.TotalIncVat);
    }

    [Fact]
    public void Rounding_happens_once_at_the_end_not_per_component()
    {
        // Components that each carry a half-cent. Rounding them individually first would
        // accumulate drift; rounding once on the inclusive figure does not.
        var request = new PriceRequest(
            offerPriceExVat: 10.005m,
            sheenUpliftFactor: 0.045m,
            tintBaseUpliftPerLitreExVat: 3.335m,
            packLitres: 3m,
            colourantSurchargeExVat: 0.005m,
            tierMultiplier: 1m,
            vat: VatRate.Standard);

        var price = PriceResolver.Resolve(request);

        var exact = (10.005m + (10.005m * 0.045m) + (3.335m * 3m) + 0.005m) * 1.15m;
        price.TotalIncVat.Should().Be(Money.Round(exact));

        // Rounding each component first would land somewhere else.
        var roundedPerComponent = Money.Round(
            (Money.Round(10.005m) + Money.Round(10.005m * 0.045m)
             + Money.Round(3.335m * 3m) + Money.Round(0.005m)) * 1.15m);
        roundedPerComponent.Should().NotBe(price.TotalIncVat);
    }

    [Fact]
    public void Zero_rated_supply_charges_no_vat()
    {
        var request = new PriceRequest(100m, 0m, 0m, 1m, 0m, 1m, VatRate.Zero);

        var price = PriceResolver.Resolve(request);

        price.VatAmount.Should().Be(0m);
        price.TotalIncVat.Should().Be(100m);
    }

    [Fact]
    public void Negative_offer_price_is_rejected()
    {
        var act = () => Request(offer: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("offerPriceExVat");
    }

    [Fact]
    public void Zero_pack_volume_is_rejected()
    {
        var act = () => Request(litres: 0m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("packLitres");
    }

    [Fact]
    public void Zero_tier_multiplier_is_rejected()
    {
        var act = () => Request(tier: 0m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("tierMultiplier");
    }
}
