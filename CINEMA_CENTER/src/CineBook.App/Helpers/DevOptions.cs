using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CineBook.Helpers;

/// <summary>
/// Tuỳ chọn dòng lệnh dành cho dev/QA (không ảnh hưởng người dùng thường):
///   --page=auth|signup|home|cinemas|categories|movie:m01|booking:{showtimeId}   mở thẳng một trang
///   --shot=duongdan.png [--delay=4000]                                       chụp cửa sổ rồi thoát (để kiểm tra giao diện tự động)
/// </summary>
public class DevOptions
{
    public string? Page { get; private init; }
    public string? ShotPath { get; private init; }
    public int DelayMs { get; private init; } = 4000;

    public static DevOptions Parse(string[] args)
    {
        string? Get(string key) => args.FirstOrDefault(a => a.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))?[(key.Length + 1)..];
        return new DevOptions { Page = Get("--page"), ShotPath = Get("--shot"), DelayMs = int.TryParse(Get("--delay"), out var d) ? d : 4000 };
    }

    /// <summary>Nếu có --shot: chờ rồi chụp window ra PNG và thoát.</summary>
    public void ScheduleScreenshot(Window window)
    {
        if (string.IsNullOrEmpty(ShotPath)) return;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DelayMs) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                var content = (FrameworkElement)window.Content;
                if (Environment.GetEnvironmentVariable("CB_DUMP") == "1") VisualDump.Print(content);
                int w = (int)content.ActualWidth, h = (int)content.ActualHeight;
                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(content);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using var fs = File.Create(ShotPath!);
                enc.Save(fs);
            }
            finally { Application.Current.Shutdown(); }
        };
        timer.Start();
    }
}

/// <summary>Dev: in cây visual (loại, kích thước, hiển thị) ra console để dò lỗi layout.</summary>
public static class VisualDump
{
    public static void Print(DependencyObject root, int depth = 0, int maxDepth = 40)
    {
        if (depth > maxDepth) return;
        var fe = root as FrameworkElement;
        Console.WriteLine($"{new string(' ', depth)}{root.GetType().Name} {(fe is null ? "" : $"{fe.ActualWidth:0}x{fe.ActualHeight:0} vis={fe.Visibility}")}");
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) Print(VisualTreeHelper.GetChild(root, i), depth + 1, maxDepth);
    }
}
