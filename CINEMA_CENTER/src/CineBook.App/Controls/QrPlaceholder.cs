using System.Windows;
using System.Windows.Media;

namespace CineBook.Controls;

/// <summary>
/// Mã QR GIẢ (placeholder): vẽ lưới 25×25 ô ngẫu nhiên theo seed từ mã vé + 3 ô định vị ở góc.
/// KHÔNG quét được. TODO-BACKEND: backend trả QR payload (hoặc URL ảnh QR) trong Booking, frontend sẽ thay bằng thư viện QR thật.
/// </summary>
public class QrPlaceholder : FrameworkElement
{
    private const int N = 25;

    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code), typeof(string), typeof(QrPlaceholder), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public string? Code { get => (string?)GetValue(CodeProperty); set => SetValue(CodeProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        double cell = Math.Min(ActualWidth, ActualHeight) / N;
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, cell * N, cell * N));

        // Seed ổn định theo mã vé để mỗi vé có hoa văn riêng.
        int seed = 17;
        foreach (var ch in Code ?? "") seed = unchecked(seed * 31 + ch);
        var rnd = new Random(seed);

        bool InFinder(int x, int y) => (x < 8 && y < 8) || (x >= N - 8 && y < 8) || (x < 8 && y >= N - 8);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                if (!InFinder(x, y) && rnd.Next(100) < 48)
                    dc.DrawRectangle(Brushes.Black, null, new Rect(x * cell, y * cell, cell, cell));

        void Finder(int ox, int oy)
        {
            dc.DrawRectangle(Brushes.Black, null, new Rect(ox * cell, oy * cell, 7 * cell, 7 * cell));
            dc.DrawRectangle(Brushes.White, null, new Rect((ox + 1) * cell, (oy + 1) * cell, 5 * cell, 5 * cell));
            dc.DrawRectangle(Brushes.Black, null, new Rect((ox + 2) * cell, (oy + 2) * cell, 3 * cell, 3 * cell));
        }
        Finder(0, 0); Finder(N - 7, 0); Finder(0, N - 7);
    }
}
