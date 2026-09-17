using Domain.Common;

namespace Domain.Sellers;

/// <summary>
/// A manufacturer or merchant selling through the platform. Stevensons is the first.
/// </summary>
public class Seller : BaseEntity
{
    public string LegalName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? CompanyRegistrationNumber { get; set; }

    public string? VatNumber { get; set; }

    /// <summary>Commission as a fraction of net revenue, e.g. 0.25m for 25%.</summary>
    public decimal CommissionRate { get; set; }

    public FulfilmentMode DefaultFulfilmentMode { get; set; } = FulfilmentMode.SellerDropship;

    public bool IsActive { get; set; } = true;
}
