namespace Domain.Colours;

/// <summary>
/// Hue grouping used for browsing and filtering. Derived from the colour value rather than
/// supplied by the manufacturer, so it is presentation metadata, not a commercial fact.
/// </summary>
public enum HueFamily
{
    Neutral,
    Red,
    Orange,
    Yellow,
    Green,
    Teal,
    Blue,
    Purple,
    Pink,
}
