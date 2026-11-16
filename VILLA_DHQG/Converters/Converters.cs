using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DOAN.Converters;

/// <summary>
/// Chuyển đường dẫn ảnh (URL http/https hoặc đường dẫn trong project, ví dụ "Images/phong1.jpg")
/// thành BitmapImage. ConverterParameter = chiều rộng giải mã (px) để tiết kiệm bộ nhớ.
/// </summary>
public class UrlToImageConverter : IValueConverter
{
    private static readonly Dictionary<string, BitmapImage> Cache = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path)) return null;

        int width = 0;
        if (parameter != null) int.TryParse(parameter.ToString(), out width);

        string key = path + "|" + width;
        if (Cache.TryGetValue(key, out var cached)) return cached;

        try
        {
            Uri uri = path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(path, UriKind.Absolute)
                : new Uri("pack://application:,,,/" + path.TrimStart('/'), UriKind.Absolute);

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            if (width > 0) bmp.DecodePixelWidth = width;
            bmp.EndInit();

            Cache[key] = bmp;
            return bmp;
        }
        catch
        {
            return null; // ảnh lỗi -> hiển thị nền placeholder
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
