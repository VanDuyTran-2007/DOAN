using System.Globalization;
using System.Windows.Data;

namespace CineBook.Converters;

/// <summary>
/// double -> "0.0" with a dot as decimal separator, regardless of the machine's region
/// (8 -> "8.0", 0 -> "0.0"). Used only by RatingBadge; the model is not changed.
/// </summary>
public class ScoreTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is double d ? d.ToString("0.0", CultureInfo.InvariantCulture) : "—";
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>
/// Repeats the bound text separated by spaces, e.g. "T18" -> "T18 T18 T18".
/// Single-character codes (P, K) repeat more times so the strip stays filled.
/// Used only by AgeBadge for the thin strip at the bottom.
/// </summary>
public class RepeatTextConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        string text = value?.ToString() ?? "";
        if (text.Length == 0) return "";
        int count = text.Length == 1 ? 6 : 3;
        return string.Join(" ", Enumerable.Repeat(text, count));
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
