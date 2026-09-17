namespace Domain.Sellers;

/// <summary>How stock reaches the customer. Persisted as the string name.</summary>
public enum FulfilmentMode
{
    /// <summary>The seller holds stock and ships direct. The launch arrangement.</summary>
    SellerDropship,

    /// <summary>We hold stock and ship.</summary>
    Warehouse,

    /// <summary>Collected by the customer from the seller.</summary>
    Collection,
}
