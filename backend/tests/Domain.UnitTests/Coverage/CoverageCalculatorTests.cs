using Domain.Coverage;
using FluentAssertions;
using Xunit;

namespace Domain.UnitTests.Coverage;

public class CoverageCalculatorTests
{
    private static readonly Guid OneLitre = Guid.NewGuid();
    private static readonly Guid FiveLitre = Guid.NewGuid();
    private static readonly Guid TwentyLitre = Guid.NewGuid();

    // Architect Premium Interior, white base, inclusive of VAT.
    private static readonly PackOption[] ArchitectPacks =
    [
        new(OneLitre, 1m, 217.35m),
        new(FiveLitre, 5m, 861.35m),
        new(TwentyLitre, 20m, 3000.35m),
    ];

    [Theory]
    [InlineData(60, 2, 8, 15)]
    [InlineData(100, 1, 8, 12.5)]
    [InlineData(45, 3, 6, 22.5)]
    public void Litres_required_is_area_times_coats_over_coverage(
        decimal area, int coats, decimal coveragePerLitre, decimal expected)
    {
        CoverageCalculator.LitresRequired(area, coats, coveragePerLitre).Should().Be(expected);
    }

    [Fact]
    public void Plan_covers_the_requirement()
    {
        var plan = CoverageCalculator.CheapestPlan(ArchitectPacks, 15m);

        plan.Should().NotBeNull();
        plan!.LitresSupplied.Should().BeGreaterThanOrEqualTo(15m);
    }

    [Fact]
    public void Three_five_litre_tins_beat_one_twenty_litre_tin_on_price()
    {
        var plan = CoverageCalculator.CheapestPlan(ArchitectPacks, 15m)!;

        plan.Selections.Should().ContainSingle();
        plan.Selections[0].Pack.ProductVariantId.Should().Be(FiveLitre);
        plan.Selections[0].Quantity.Should().Be(3);
        plan.TotalIncVat.Should().Be(2584.05m);
        plan.SurplusLitres.Should().Be(0m);
    }

    [Fact]
    public void Mixed_pack_sizes_are_used_when_that_is_cheapest()
    {
        // 11 litres: 2 x 5L plus 1 x 1L beats 3 x 5L.
        var plan = CoverageCalculator.CheapestPlan(ArchitectPacks, 11m)!;

        plan.TotalIncVat.Should().Be(1940.05m);
        plan.LitresSupplied.Should().Be(11m);
        plan.SurplusLitres.Should().Be(0m);
    }

    [Fact]
    public void Twenty_litre_tin_wins_once_it_is_actually_cheaper()
    {
        var packs = new PackOption[]
        {
            new(FiveLitre, 5m, 861.35m),
            new(TwentyLitre, 20m, 2400.00m), // genuinely better value per litre
        };

        var plan = CoverageCalculator.CheapestPlan(packs, 18m)!;

        plan.Selections.Should().ContainSingle();
        plan.Selections[0].Pack.ProductVariantId.Should().Be(TwentyLitre);
        plan.TotalIncVat.Should().Be(2400.00m);
    }

    [Fact]
    public void Ties_on_cost_break_towards_the_smaller_surplus()
    {
        // Two 5L and one 10L cost the same per litre, so covering 10L exactly must not
        // sell the customer a spare tin.
        var ten = Guid.NewGuid();
        var packs = new PackOption[]
        {
            new(FiveLitre, 5m, 500m),
            new(ten, 10m, 1000m),
        };

        var plan = CoverageCalculator.CheapestPlan(packs, 10m)!;

        plan.TotalIncVat.Should().Be(1000m);
        plan.SurplusLitres.Should().Be(0m);
    }

    [Fact]
    public void Small_requirement_buys_the_smallest_pack()
    {
        var plan = CoverageCalculator.CheapestPlan(ArchitectPacks, 0.8m)!;

        plan.Selections.Should().ContainSingle();
        plan.Selections[0].Pack.ProductVariantId.Should().Be(OneLitre);
        plan.SurplusLitres.Should().Be(0.2m);
    }

    [Fact]
    public void No_packs_means_no_plan()
    {
        CoverageCalculator.CheapestPlan([], 10m).Should().BeNull();
    }

    [Theory]
    [InlineData(0, 2, 8)]
    [InlineData(-5, 2, 8)]
    public void Non_positive_area_is_rejected(decimal area, int coats, decimal coverage)
    {
        var act = () => CoverageCalculator.LitresRequired(area, coats, coverage);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("areaSquareMetres");
    }

    [Fact]
    public void Zero_coats_is_rejected()
    {
        var act = () => CoverageCalculator.LitresRequired(60m, 0, 8m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("coats");
    }

    [Fact]
    public void Zero_coverage_is_rejected()
    {
        var act = () => CoverageCalculator.LitresRequired(60m, 2, 0m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("coveragePerLitre");
    }
}
