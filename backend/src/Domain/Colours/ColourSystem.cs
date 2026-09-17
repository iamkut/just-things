using Domain.Common;

namespace Domain.Colours;

/// <summary>
/// A named colour collection -- Stevensons Real Colours, Standard Colours, NCS, RAL.
/// Proprietary systems belong to a seller; open standards do not.
/// </summary>
public class ColourSystem : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    /// <summary>The seller that owns this system, or null for an open standard such as RAL.</summary>
    public Guid? OwnerSellerId { get; set; }

    public bool IsProprietary { get; set; }

    public ICollection<Colour> Colours { get; set; } = [];
}
