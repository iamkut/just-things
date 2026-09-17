using System.Globalization;

namespace Shared.Formatting;

/// <summary>
/// South African rand formatting. Space as the thousands separator and comma as the decimal
/// separator, which is the SI convention South Africa uses: R 1 234,56.
/// </summary>
public static class Zar
{
    private static readonly NumberFormatInfo Rand = new()
    {
        CurrencySymbol = "R",
        CurrencyDecimalSeparator = ",",
        CurrencyGroupSeparator = "\u00a0", // non-breaking space, so a price never wraps mid-number
        CurrencyDecimalDigits = 2,
        CurrencyGroupSizes = [3],
        CurrencyPositivePattern = 2, // R 1 234,56
        CurrencyNegativePattern = 9, // R -1 234,56
    };

    public static string Format(decimal amount) => amount.ToString("C", Rand);
}
