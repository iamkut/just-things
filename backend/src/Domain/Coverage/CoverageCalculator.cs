using Domain.Common;

namespace Domain.Coverage;

/// <summary>
/// Turns an area into a basket. Pure -- no I/O -- so it is unit-testable.
/// </summary>
/// <remarks>
/// This is the highest-converting feature in paint retail: a customer who is told
/// "2 x 5L and 1 x 1L" buys, where one told "you need 11.3 litres" goes to a store.
/// </remarks>
public static class CoverageCalculator
{
    /// <summary>Litres needed to cover an area, rounded up to a sensible precision.</summary>
    public static decimal LitresRequired(decimal areaSquareMetres, int coats, decimal coveragePerLitre)
    {
        if (areaSquareMetres <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(areaSquareMetres), areaSquareMetres, "Area must be greater than zero.");
        }

        if (coats <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(coats), coats, "Coats must be at least one.");
        }

        if (coveragePerLitre <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coveragePerLitre), coveragePerLitre, "Coverage per litre must be greater than zero.");
        }

        return Math.Round(areaSquareMetres * coats / coveragePerLitre, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// The cheapest combination of pack sizes covering <paramref name="litresRequired"/>.
    /// Ties on cost break towards the smaller surplus, so a customer is never sold a spare
    /// 20L tin when two 5L tins cost the same.
    /// </summary>
    /// <returns>Null when no combination can cover the requirement.</returns>
    public static CoveragePlan? CheapestPlan(IReadOnlyList<PackOption> packs, decimal litresRequired)
    {
        ArgumentNullException.ThrowIfNull(packs);

        if (litresRequired <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(litresRequired), litresRequired, "Litres required must be greater than zero.");
        }

        var options = packs.Where(p => p.Litres > 0m).OrderByDescending(p => p.Litres).ToArray();
        if (options.Length == 0)
        {
            return null;
        }

        var counts = new int[options.Length];
        int[]? bestCounts = null;
        var bestCost = decimal.MaxValue;
        var bestLitres = decimal.MaxValue;

        void Walk(int index, decimal litresSoFar, decimal costSoFar)
        {
            if (costSoFar > bestCost)
            {
                return;
            }

            if (index == options.Length)
            {
                if (litresSoFar < litresRequired)
                {
                    return;
                }

                if (costSoFar < bestCost || (costSoFar == bestCost && litresSoFar < bestLitres))
                {
                    bestCost = costSoFar;
                    bestLitres = litresSoFar;
                    bestCounts = (int[])counts.Clone();
                }

                return;
            }

            // One more than strictly needed, so a run of the smaller pack can still win on price.
            var cap = (int)Math.Ceiling(litresRequired / options[index].Litres) + 1;
            for (var n = 0; n <= cap; n++)
            {
                counts[index] = n;
                Walk(index + 1, litresSoFar + (n * options[index].Litres),
                     costSoFar + (n * options[index].UnitPriceIncVat));
            }

            counts[index] = 0;
        }

        Walk(0, 0m, 0m);

        if (bestCounts is null)
        {
            return null;
        }

        var selections = options
            .Select((pack, i) => new PackSelection(pack, bestCounts[i]))
            .Where(s => s.Quantity > 0)
            .ToArray();

        return new CoveragePlan
        {
            LitresRequired = litresRequired,
            Selections = selections,
            LitresSupplied = bestLitres,
            TotalIncVat = Money.Round(bestCost),
        };
    }
}
