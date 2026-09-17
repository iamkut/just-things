namespace Domain.Ordering;

/// <summary>
/// Decides returnability for a line. Pure -- no I/O -- so it is unit-testable.
/// </summary>
/// <remarks>
/// This is a per-line property, not a per-order one: a basket can hold a tinted 5L that cannot
/// come back beside a roller tray that can. It must be disclosed at add-to-cart, not buried in
/// terms and conditions.
/// </remarks>
public static class ReturnabilityPolicy
{
    /// <summary>
    /// A line is made to order exactly when a colour was chosen, because that is the point at
    /// which the tin is mixed to the customer specification.
    /// </summary>
    public static Returnability For(bool isMadeToOrder) =>
        isMadeToOrder ? Returnability.NonReturnableCustomMixed : Returnability.Returnable;

    /// <summary>True when the customer may cancel within the ECTA cooling-off window.</summary>
    public static bool AllowsCoolingOffReturn(Returnability returnability) =>
        returnability == Returnability.Returnable;

    /// <summary>
    /// Always true. Goods that do not conform to the specification ordered remain returnable
    /// however they were made, and no line may opt out of that.
    /// </summary>
    public static bool AllowsDefectReturn(Returnability returnability) => true;
}
