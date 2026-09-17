using Domain.Common;

namespace Domain.Catalogue;

/// <summary>A manufacturer brand -- Stevensons, Crest, Durawood. A seller may carry several.</summary>
public class Brand : BaseEntity
{
    public Guid SellerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}
