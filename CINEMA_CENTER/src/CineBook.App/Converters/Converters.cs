using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CineBook.Models;

namespace CineBook.Converters;

/// <summary>true → Collapsed, false → Visible.</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is not true;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => value is not true;
}

/// <summary>null hoặc chuỗi rỗng → Collapsed.</summary>
public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is null || value is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>null hoặc chuỗi rỗng → Visible (ngược lại Collapsed). Dùng cho placeholder.</summary>
public class NullToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is null || value is string { Length: 0 } ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>So sánh value.ToString() với ConverterParameter → bool. Dùng để đánh dấu mục menu đang active.</summary>
public class EqualsConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => string.Equals(value?.ToString(), p?.ToString(), StringComparison.Ordinal);
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>"#RRGGBB" → SolidColorBrush. Dùng cho badge màu theo hãng rạp và poster placeholder (màu đến từ dữ liệu).</summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)value)); }
        catch { return (Brush)Application.Current.FindResource("Burgundy"); }
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>decimal → "95.000 ₫".</summary>
public class MoneyConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is decimal d ? Labels.Money(d) : "";
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>AgeRating → màu nhãn tuổi lấy từ theme.</summary>
public class AgeRatingBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        string key = value is AgeRating a ? a switch
        {
            AgeRating.P or AgeRating.K => "Success",
            AgeRating.T13 => "AccentGold",
            AgeRating.T16 => "BurgundyHover",
            _ => "Error"
        } : "TextMuted";
        return (Brush)Application.Current.FindResource(key);
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>int → Visible nếu lớn hơn 0.</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is int n && n > 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>
/// Movie → brush poster. Chưa có PosterUrl thật thì sinh gradient từ màu placeholder (không dùng poster có bản quyền).
/// </summary>
public class PosterBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value is not Movie m) return Brushes.Transparent;
        if (!string.IsNullOrWhiteSpace(m.PosterUrl) && Uri.TryCreate(m.PosterUrl, UriKind.Absolute, out var uri))
            return new ImageBrush(new System.Windows.Media.Imaging.BitmapImage(uri)) { Stretch = Stretch.UniformToFill };
        return Gradient(m.PlaceholderColorHex, new Point(0, 0), new Point(1, 1));
    }

    internal static Brush Gradient(string hex, Point start, Point end)
    {
        Color baseColor;
        try { baseColor = (Color)ColorConverter.ConvertFromString(hex); } catch { baseColor = (Color)Application.Current.FindResource("BurgundyColor"); }
        var dark = (Color)Application.Current.FindResource("NavyDarkestColor");
        var b = new LinearGradientBrush(baseColor, dark, start, end);
        b.Freeze();
        return b;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Movie → brush backdrop (ngang): gradient từ màu phim sang navy.</summary>
public class BackdropBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is Movie m ? PosterBrushConverter.Gradient(m.PlaceholderColorHex, new Point(0, 0), new Point(1, 0.8)) : Brushes.Transparent;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Chuỗi rỗng/null → false.</summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => !string.IsNullOrEmpty(value as string);
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>int (bước hiện tại) == ConverterParameter → Visible, ngược lại Collapsed. Dùng cho wizard đặt vé.</summary>
public class StepVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is int n && int.TryParse(p?.ToString(), out var step) && n == step ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>[showtimeId, selectedShowtime] → true nếu Id trùng. Dùng để tô sáng giờ chiếu đang chọn ở bước ①.</summary>
public class ShowtimeSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
        => values.Length == 2 && values[0] is string id && values[1] is Showtime s && s.Id == id;
    public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Chữ cái đầu của chuỗi (logo placeholder của phương thức thanh toán).</summary>
public class FirstLetterConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is string { Length: > 0 } s ? s[..1].ToUpperInvariant() : "";
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
