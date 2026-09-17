using Domain.Common;

namespace Domain.Verticals;

/// <summary>
/// A vertical storefront -- Just Paints, Just Tools. Verticals are data, not deployments:
/// adding one is a row plus a DNS record, never a fork. See ADR-0003.
/// </summary>
public class Vertical : BaseEntity
{
    /// <summary>Display name, e.g. "Just Paints".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL slug, e.g. "paints".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Host this vertical is served on, e.g. "paints.justthings.co.za".</summary>
    public string Hostname { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
