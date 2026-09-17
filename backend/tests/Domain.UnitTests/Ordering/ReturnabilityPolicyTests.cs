using Domain.Ordering;
using FluentAssertions;
using Xunit;

namespace Domain.UnitTests.Ordering;

public class ReturnabilityPolicyTests
{
    [Fact]
    public void Tinted_lines_lose_the_cooling_off_right()
    {
        var returnability = ReturnabilityPolicy.For(isMadeToOrder: true);

        returnability.Should().Be(Returnability.NonReturnableCustomMixed);
        ReturnabilityPolicy.AllowsCoolingOffReturn(returnability).Should().BeFalse();
    }

    [Fact]
    public void Untinted_lines_keep_the_cooling_off_right()
    {
        var returnability = ReturnabilityPolicy.For(isMadeToOrder: false);

        returnability.Should().Be(Returnability.Returnable);
        ReturnabilityPolicy.AllowsCoolingOffReturn(returnability).Should().BeTrue();
    }

    [Theory]
    [InlineData(Returnability.Returnable)]
    [InlineData(Returnability.NonReturnableCustomMixed)]
    public void Defect_rights_survive_regardless(Returnability returnability)
    {
        // Goods that do not match what was ordered come back however they were made.
        ReturnabilityPolicy.AllowsDefectReturn(returnability).Should().BeTrue();
    }

    [Fact]
    public void Returnability_is_per_line_so_one_basket_can_hold_both()
    {
        var order = new Order
        {
            Lines =
            [
                new OrderLine
                {
                    ProductName = "Architect Premium Interior",
                    Configuration = new LineConfiguration { IsMadeToOrder = true, ColourCode = "RC53" },
                    Returnability = ReturnabilityPolicy.For(true),
                },
                new OrderLine
                {
                    ProductName = "Roller tray",
                    Configuration = new LineConfiguration { IsMadeToOrder = false },
                    Returnability = ReturnabilityPolicy.For(false),
                },
            ],
        };

        order.HasMadeToOrderLines.Should().BeTrue();
        order.Lines.Select(l => l.Returnability).Should().BeEquivalentTo(
            [Returnability.NonReturnableCustomMixed, Returnability.Returnable]);
    }
}
