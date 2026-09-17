using Domain.Common;

namespace Domain.Ordering;

/// <summary>A basket, belonging to a customer or to an anonymous session.</summary>
public class Cart : BaseEntity
{
    public Guid VerticalId { get; set; }

    public Guid? CustomerId { get; set; }

    /// <summary>Session key for a guest basket.</summary>
    public string? AnonymousId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CartLine> Lines { get; set; } = [];

    public decimal TotalIncVat => Lines.Sum(l => l.LineTotalIncVat);
}
