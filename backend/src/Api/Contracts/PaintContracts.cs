namespace Api.Contracts;

public sealed record ColourResponse(
    Guid Id,
    string Code,
    string Name,
    string? Hex,
    decimal? Lrv,
    string HueFamily);

public sealed record PriceBreakdownResponse(
    decimal BasePriceExVat,
    decimal SheenUpliftExVat,
    decimal TintUpliftExVat,
    decimal ColourantSurchargeExVat,
    decimal TierAdjustmentExVat,
    decimal SubtotalExVat,
    decimal VatAmount,
    decimal TotalIncVat,
    string TotalDisplay);

public sealed record PricedVariantResponse(
    Guid ProductVariantId,
    string Sku,
    string Sheen,
    decimal PackLitres,
    decimal WeightKg,
    bool InStock,
    PriceBreakdownResponse Price);

public sealed record ProductPricingResponse(
    Guid ProductId,
    string ProductName,
    decimal CoveragePerLitre,
    int CoatsRecommended,
    string? ColourCode,
    string? ColourName,
    string? ColourHex,
    string? TintBaseName,
    bool IsMadeToOrder,
    string Returnability,
    string ReturnabilityNotice,
    IReadOnlyList<PricedVariantResponse> Variants);

public sealed record CoverageRequest(
    decimal AreaSquareMetres,
    int Coats,
    string Sheen,
    string? ColourCode);

public sealed record CoverageLineResponse(
    Guid ProductVariantId,
    decimal PackLitres,
    int Quantity,
    decimal UnitPriceIncVat,
    decimal LineTotalIncVat);

public sealed record CoverageResponse(
    decimal LitresRequired,
    decimal LitresSupplied,
    decimal SurplusLitres,
    decimal TotalIncVat,
    string TotalDisplay,
    IReadOnlyList<CoverageLineResponse> Lines);
