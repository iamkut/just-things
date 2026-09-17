using Domain.Common;

namespace Domain.Colours;

/// <summary>
/// A tinting base -- White, Pastel, Medium, Deep, Accent. The base is what a colour is
/// actually mixed into, and it is the reason price varies with colour: a deep base carries
/// more colourant and less titanium dioxide than a white one, and costs more per litre.
/// </summary>
public class TintBase : BaseEntity
{
    public Guid SellerId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Price added per litre of pack volume, exclusive of VAT. Zero for a white base.
    /// </summary>
    public decimal UpliftPerLitreExVat { get; set; }

    public int SortOrder { get; set; }
}
