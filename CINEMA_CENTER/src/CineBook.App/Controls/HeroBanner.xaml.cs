using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CineBook.ViewModels;

namespace CineBook.Controls;

public partial class HeroBanner : UserControl
{
    private Point? _press;
    private bool _dragging;
    private bool _settling;
    private int _direction;

    public HeroBanner()
    {
        InitializeComponent();
        Unloaded += (_, _) => ClearSlides();
    }

    private void OnPointerDown(object sender, MouseButtonEventArgs e)
    {
        if (_settling || DataContext is not HomeViewModel vm || vm.HeroAtOffset(1) is null) return;
        for (var element = e.OriginalSource as DependencyObject; element is not null && element != Stage;
             element = element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element))
        {
            if (element is ButtonBase) return;
        }
        _press = e.GetPosition(Stage);
    }

    private void OnPointerMove(object sender, MouseEventArgs e)
    {
        if (_press is not { } start || e.LeftButton != MouseButtonState.Pressed || DataContext is not HomeViewModel vm) return;
        var delta = e.GetPosition(Stage) - start;
        if (!_dragging)
        {
            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance || Math.Abs(delta.X) < Math.Abs(delta.Y)) return;
            if (!Stage.CaptureMouse()) return;
            _dragging = true;
            vm.PauseHeroRotation();
            Stage.Cursor = Cursors.ScrollWE;
        }

        var offset = Math.Clamp(delta.X, -Stage.ActualWidth, Stage.ActualWidth);
        var direction = offset < 0 ? 1 : -1;
        if (_direction != direction)
        {
            _direction = direction;
            IncomingSlide.Content = vm.HeroAtOffset(direction);
        }
        CurrentSlideOffset.X = offset;
        IncomingSlideOffset.X = offset + direction * Stage.ActualWidth;
        e.Handled = true;
    }

    private void OnPointerUp(object sender, MouseButtonEventArgs e)
    {
        _press = null;
        if (!_dragging) return;
        _dragging = false;
        Stage.ReleaseMouseCapture();
        e.Handled = true;
        Settle(Math.Abs(CurrentSlideOffset.X) >= Math.Min(110, Stage.ActualWidth * 0.18));
    }

    private void OnLostCapture(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        _press = null;
        _dragging = false;
        Settle(commit: false);
    }

    private void Settle(bool commit)
    {
        _settling = true;
        var width = Stage.ActualWidth;
        var direction = _direction;
        var destination = commit ? -direction * width : 0;

        void Complete()
        {
            if (!_settling) return;
            if (IsLoaded && DataContext is HomeViewModel vm) vm.MoveHero(commit ? direction : 0);
            ClearSlides();
        }

        if (!SystemParameters.ClientAreaAnimation)
        {
            Complete();
            return;
        }

        var duration = TimeSpan.FromMilliseconds(210);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var slide = new DoubleAnimation(CurrentSlideOffset.X, destination, duration) { EasingFunction = ease };
        slide.Completed += (_, _) => Complete();
        IncomingSlideOffset.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(IncomingSlideOffset.X, destination + direction * width, duration) { EasingFunction = ease });
        CurrentSlideOffset.BeginAnimation(TranslateTransform.XProperty, slide);
    }

    private void ClearSlides()
    {
        _press = null;
        _dragging = _settling = false;
        _direction = 0;
        if (Stage.IsMouseCaptured) Stage.ReleaseMouseCapture();
        Stage.ClearValue(CursorProperty);
        CurrentSlideOffset.BeginAnimation(TranslateTransform.XProperty, null);
        IncomingSlideOffset.BeginAnimation(TranslateTransform.XProperty, null);
        CurrentSlideOffset.X = IncomingSlideOffset.X = 0;
        IncomingSlide.Content = null;
    }
}
