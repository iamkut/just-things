using Domain.Common;

namespace Domain.Colours;

/// <summary>
/// A single colour within a system, e.g. RC4 Guinness.
/// </summary>
/// <remarks>
/// <see cref="Hex"/> and <see cref="Lrv"/> are nullable on purpose. A manufacturer may publish
/// names and codes long before it shares colour values, and the catalogue has to sell before
/// that data is complete: a colour without a hex renders from a swatch image, and one without
/// an LRV simply drops out of the light-reflectance filter.
/// </remarks>
public class Colour : BaseEntity
{
    public Guid ColourSystemId { get; set; }

    public ColourSystem? ColourSystem { get; set; }

    /// <summary>Manufacturer code, e.g. "RC4".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Manufacturer name, e.g. "Guinness".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>sRGB value as "#rrggbb", lowercase. Null until supplied or measured.</summary>
    public string? Hex { get; set; }

    /// <summary>Light reflectance value, 0 to 100. Null until supplied or derived.</summary>
    public decimal? Lrv { get; set; }

    public HueFamily HueFamily { get; set; } = HueFamily.Neutral;

    public bool IsDiscontinued { get; set; }

    public int SortOrder { get; set; }
}
