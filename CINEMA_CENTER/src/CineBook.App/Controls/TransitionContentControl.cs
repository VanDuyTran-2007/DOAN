using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CineBook.Controls;

/// <summary>
/// ContentControl có hiệu ứng chuyển trang: fade-in + trượt nhẹ từ dưới lên (200ms).
/// Code-behind này chỉ làm animation (thuần UI), không có logic nghiệp vụ.
/// </summary>
public class TransitionContentControl : ContentControl
{
    public TransitionContentControl()
    {
        RenderTransform = new TranslateTransform();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        if (newContent is null) return;

        var duration = TimeSpan.FromMilliseconds(200);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        ((TranslateTransform)RenderTransform).BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(14, 0, duration) { EasingFunction = ease });
    }
}
