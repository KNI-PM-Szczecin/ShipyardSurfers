using System.Globalization;

public static class ScoreFormatter
{
    private static readonly NumberFormatInfo NumberFormat = CreateFormat();

    public static string Format(float score)
    {
        return ((long)score).ToString("N0", NumberFormat);
    }

    private static NumberFormatInfo CreateFormat()
    {
        var info = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        info.NumberGroupSeparator = " ";
        return info;
    }
}
