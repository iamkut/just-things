namespace Domain.Coverage;

/// <summary>How many of one pack size to buy.</summary>
public readonly record struct PackSelection(PackOption Pack, int Quantity)
{
    public decimal LineTotalIncVat => Pack.UnitPriceIncVat * Quantity;

    public decimal Litres => Pack.Litres * Quantity;
}

/// <summary>The cheapest way to cover the litres required.</summary>
public sealed record CoveragePlan
{
    public required decimal LitresRequired { get; init; }

    public required IReadOnlyList<PackSelection> Selections { get; init; }

    public required decimal LitresSupplied { get; init; }

    /// <summary>Litres bought beyond what the job needs.</summary>
    public decimal SurplusLitres => LitresSupplied - LitresRequired;

    public required decimal TotalIncVat { get; init; }
}
