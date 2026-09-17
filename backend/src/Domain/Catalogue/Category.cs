using Domain.Common;

namespace Domain.Catalogue;

/// <summary>A category in a vertical tree -- Interior walls, Roof, Wood care.</summary>
public class Category : BaseEntity
{
    public Guid VerticalId { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
