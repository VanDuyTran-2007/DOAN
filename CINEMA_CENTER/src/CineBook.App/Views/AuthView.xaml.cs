using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CineBook.Views;

/// <summary>Code-behind thuần UI: căn vùng kính mờ và animation chuyển tab. Không có logic nghiệp vụ.</summary>
public partial class AuthView : UserControl
{
    private const double BlurBleed = 40; // BlurLayer lớn hơn panel 40px mỗi phía (Margin = -40)
    private readonly VisualBrush _blurBrush;

    public AuthView()
    {
        InitializeComponent();

        // VisualBrush "chụp" BackgroundLayer đang chạy; Viewbox sẽ được chỉnh theo vị trí panel.
        _blurBrush = new VisualBrush(BackgroundLayer)
        {
            ViewboxUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.Fill,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top
        };
        BlurLayer.Background = _blurBrush;
        Loaded += (_, _) => UpdateGlass();
    }

    private void OnLayoutChanged(object sender, SizeChangedEventArgs e) => UpdateGlass();

    private void UpdateGlass()
    {
        if (GlassPanel.ActualWidth <= 0 || GlassPanel.ActualHeight <= 0 || BackgroundLayer.ActualWidth <= 0) return;

        // Vị trí góc trên-trái của panel trong hệ toạ độ của lớp nền.
        var topLeft = GlassPanel.TransformToVisual(BackgroundLayer).Transform(new Point(0, 0));
        _blurBrush.Viewbox = new Rect(topLeft.X - BlurBleed, topLeft.Y - BlurBleed,
                                      GlassPanel.ActualWidth + BlurBleed * 2, GlassPanel.ActualHeight + BlurBleed * 2);

        // Cắt bo góc 16px để phần blur tràn ra ngoài không hiện.
        GlassPanel.Clip = new RectangleGeometry(new Rect(0, 0, GlassPanel.ActualWidth, GlassPanel.ActualHeight), 16, 16);
    }

    // Chuyển tab: nội dung trượt nhẹ từ phải sang + fade.
    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource != Tabs) return; // bỏ qua SelectionChanged của control con
        if (Tabs.Template.FindName("PART_SelectedContentHost", Tabs) is not ContentPresenter host) return;

        var move = new TranslateTransform();
        host.RenderTransform = move;
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(36, 0, duration) { EasingFunction = ease });
        host.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration));
        Dispatcher.BeginInvoke(UpdateGlass, System.Windows.Threading.DispatcherPriority.Loaded); // chiều cao panel đổi theo tab
    }
}
