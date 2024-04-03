using System.Globalization;

namespace CustomAlbums.Utilities;

public static class Formatting {
    public static int ParseAsInt(this string value)
        => int.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    public static float ParseAsFloat(this string value)
        => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    public static decimal ParseAsDecimal(this string value)
        => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    
    public static bool TryParseAsInt(this string value, out int result)
        => int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    
    public static bool TryParseAsFloat(this string value, out float result)
        => float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    
    public static bool TryParseAsDecimal(this string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    
    public static string ToStringInvariant(this int value, string format = "")
        => value.ToString(format, CultureInfo.InvariantCulture);
    
    public static string ToStringInvariant(this float value, string format = "")
        => value.ToString(format, CultureInfo.InvariantCulture);
    
    public static string ToStringInvariant(this decimal value, string format = "")
        => value.ToString(format, CultureInfo.InvariantCulture);

    public static bool StartsWithOrdinal(this string value, string compare)
        => value.StartsWith(compare, StringComparison.Ordinal);
}