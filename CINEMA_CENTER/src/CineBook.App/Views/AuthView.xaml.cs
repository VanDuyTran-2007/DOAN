using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CineBook.Views;

/// <summary>Hiệu ứng chuyển tab đăng nhập và đăng ký.</summary>
public partial class AuthView : UserControl
{
    public AuthView() => InitializeComponent();

    // Chuyển tab: nội dung trượt nhẹ từ phải sang + fade.
    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource != Tabs || !SystemParameters.ClientAreaAnimation) return;
        if (Tabs.Template?.FindName("PART_SelectedContentHost", Tabs) is not ContentPresenter host) return;

        var move = new TranslateTransform();
        host.RenderTransform = move;
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(36, 0, duration) { EasingFunction = ease });
        host.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration));
    }
}
