namespace Domain.Catalogue;

/// <summary>
/// Transport hazard classification. Solvent-based coatings are flammable, which restricts
/// which couriers and routes may carry them. Persisted as the string name.
/// </summary>
public enum HazardClass
{
    None,
    Flammable,
    Corrosive,
}
