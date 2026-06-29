using System.Globalization;

namespace CustomAlbums.Utilities;

public static class Formatting
{
    public static int ParseAsInt(this string value)
    {
        return int.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    public static float ParseAsFloat(this string value)
    {
        return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public static decimal ParseAsDecimal(this string value)
    {
        return decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    public static bool TryParseAsInt(this string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParseAsFloat(this string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParseAsDecimal(this string value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    public static string ToStringInvariant(this int value, string format = "")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string ToStringInvariant(this float value, string format = "")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string ToStringInvariant(this decimal value, string format = "")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}