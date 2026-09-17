namespace Domain.Common;

/// <summary>
/// Base type for all persisted entities. Every entity has a GUID primary key.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}
