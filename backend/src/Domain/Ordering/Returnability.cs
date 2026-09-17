namespace Domain.Ordering;

/// <summary>
/// Whether a line carries the ECTA seven-day cooling-off right. Fixed at order placement and
/// never recomputed: the rule that applied is the rule at the time of sale. Persisted as the
/// string name.
/// </summary>
public enum Returnability
{
    /// <summary>Stocked goods. The cooling-off right applies.</summary>
    Returnable,

    /// <summary>
    /// Custom-tinted paint. ECTA excludes goods made to a consumer specification, so the
    /// cooling-off right does not apply. Defect and non-conformance rights still do --
    /// see <see cref="ReturnabilityPolicy.AllowsDefectReturn"/>.
    /// </summary>
    NonReturnableCustomMixed,
}
