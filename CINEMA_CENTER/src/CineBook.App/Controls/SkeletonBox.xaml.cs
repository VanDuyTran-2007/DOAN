using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CineBook.Controls;

public partial class SkeletonBox : UserControl
{
    private static readonly DoubleAnimation Shimmer = CreateShimmer();

    public SkeletonBox()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateShimmer(IsVisible);
        Unloaded += (_, _) => UpdateShimmer(false);
        IsVisibleChanged += (_, _) => UpdateShimmer(IsLoaded && IsVisible);
    }

    private void UpdateShimmer(bool visible) =>
        Shift.BeginAnimation(TranslateTransform.XProperty, visible && SystemParameters.ClientAreaAnimation ? Shimmer : null);

    private static DoubleAnimation CreateShimmer()
    {
        var animation = new DoubleAnimation(-1, 1, TimeSpan.FromSeconds(1.4))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };
        Timeline.SetDesiredFrameRate(animation, 30);
        animation.Freeze();
        return animation;
    }
}
