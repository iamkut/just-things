namespace Domain.Coverage;

/// <summary>One pack size the customer could buy, with its resolved inclusive price.</summary>
public readonly record struct PackOption(Guid ProductVariantId, decimal Litres, decimal UnitPriceIncVat);
