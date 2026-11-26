using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DOAN.Helpers;

/// <summary>
/// Hiệu ứng hover dùng lại được trong XAML:
///   helpers:Fx.HoverZoom="1.06"  -> phóng to nhẹ phần tử (thường là ảnh) khi rê chuột
///   helpers:Fx.HoverLift="True"  -> nâng thẻ lên 6px khi rê chuột
/// </summary>
public static class Fx
{
    private static readonly Duration Speed = new(TimeSpan.FromMilliseconds(450));
    private static readonly IEasingFunction Ease = new CubicEase { EasingMode = EasingMode.EaseOut };

    // ---------- HoverZoom ----------
    public static readonly DependencyProperty HoverZoomProperty = DependencyProperty.RegisterAttached(
        "HoverZoom", typeof(double), typeof(Fx), new PropertyMetadata(1.0, OnHoverZoomChanged));

    public static double GetHoverZoom(DependencyObject d) => (double)d.GetValue(HoverZoomProperty);
    public static void SetHoverZoom(DependencyObject d, double value) => d.SetValue(HoverZoomProperty, value);

    private static void OnHoverZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el) return;
        var scale = new ScaleTransform(1, 1);
        el.RenderTransformOrigin = new Point(0.5, 0.5);
        el.RenderTransform = scale;

        // Lắng nghe trên phần tử cha để vùng hover là cả khung chứa ảnh
        el.Loaded += (_, _) =>
        {
            var host = VisualTreeHelper.GetParent(el) as UIElement ?? el;
            host.MouseEnter += (_, _) => Animate(scale, GetHoverZoom(el));
            host.MouseLeave += (_, _) => Animate(scale, 1.0);
        };
    }

    private static void Animate(ScaleTransform t, double to)
    {
        var a = new DoubleAnimation(to, Speed) { EasingFunction = Ease };
        t.BeginAnimation(ScaleTransform.ScaleXProperty, a);
        t.BeginAnimation(ScaleTransform.ScaleYProperty, a);
    }

    // ---------- HoverLift ----------
    public static readonly DependencyProperty HoverLiftProperty = DependencyProperty.RegisterAttached(
        "HoverLift", typeof(bool), typeof(Fx), new PropertyMetadata(false, OnHoverLiftChanged));

    public static bool GetHoverLift(DependencyObject d) => (bool)d.GetValue(HoverLiftProperty);
    public static void SetHoverLift(DependencyObject d, bool value) => d.SetValue(HoverLiftProperty, value);

    private static void OnHoverLiftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement el || e.NewValue is not true) return;
        var move = new TranslateTransform();
        el.RenderTransform = move;
        el.MouseEnter += (_, _) => move.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(-6, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = Ease });
        el.MouseLeave += (_, _) => move.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(250))) { EasingFunction = Ease });
    }

    /// <summary>Hiệu ứng xuất hiện (mờ -> rõ, trượt nhẹ lên) khi chuyển trang.</summary>
    public static void FadeIn(FrameworkElement el)
    {
        var move = new TranslateTransform(0, 24);
        el.RenderTransform = move;
        el.Opacity = 0;
        var d = new Duration(TimeSpan.FromMilliseconds(500));
        el.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, d) { EasingFunction = Ease });
        move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, d) { EasingFunction = Ease });
    }
}
