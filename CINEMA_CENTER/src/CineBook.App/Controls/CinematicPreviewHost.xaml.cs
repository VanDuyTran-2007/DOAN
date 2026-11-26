using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CineBook.Models;

namespace CineBook.Controls;

[ContentProperty(nameof(Gallery))]
public partial class CinematicPreviewHost : UserControl
{
    public static readonly DependencyProperty GalleryProperty = DependencyProperty.Register(
        nameof(Gallery), typeof(FrameworkElement), typeof(CinematicPreviewHost));

    public static readonly DependencyProperty BookMovieCommandProperty = DependencyProperty.Register(
        nameof(BookMovieCommand), typeof(ICommand), typeof(CinematicPreviewHost));

    private readonly DispatcherTimer _openTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _closeTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private MoviePosterCard? _pending;
    private MoviePosterCard? _active;
    private Window? _window;
    private bool _explicitPreview;

    public CinematicPreviewHost()
    {
        InitializeComponent();
        _openTimer.Tick += (_, _) =>
        {
            var card = _pending;
            CancelOpening();
            if (card?.IsMouseOver == true) Open(card, explicitPreview: false);
        };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            if (_active?.IsMouseOver != true && !PreviewPanel.IsMouseOver && !PreviewPanel.IsKeyboardFocusWithin)
                Close();
        };
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => Dismiss();
        AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnGalleryScroll));
        AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnMouseDown), true);
        AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnKeyDown), true);
    }

    public FrameworkElement? Gallery
    {
        get => (FrameworkElement?)GetValue(GalleryProperty);
        set => SetValue(GalleryProperty, value);
    }

    public ICommand? BookMovieCommand
    {
        get => (ICommand?)GetValue(BookMovieCommandProperty);
        set => SetValue(BookMovieCommandProperty, value);
    }

    internal static CinematicPreviewHost? FindFor(MoviePosterCard card)
    {
        for (DependencyObject? parent = card; parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is CinematicPreviewHost host) return host;
        return null;
    }

    internal void PointerEntered(MoviePosterCard card)
    {
        if (_explicitPreview || card == _active)
        {
            if (card == _active) _closeTimer.Stop();
            return;
        }
        CancelOpening();
        _pending = card;
        _openTimer.Start();
    }

    internal void PointerLeft(MoviePosterCard card)
    {
        if (_pending == card) CancelOpening();
        if (_active == card) ScheduleClose();
    }

    internal void ShowForInput(MoviePosterCard card)
    {
        CancelOpening();
        if (_active == card)
        {
            _explicitPreview = true;
            _closeTimer.Stop();
            DetailAction.Focus();
            return;
        }
        Open(card, explicitPreview: true);
    }

    internal void Forget(MoviePosterCard card)
    {
        if (_pending == card) CancelOpening();
        if (_active == card) Close();
    }

    private void Open(MoviePosterCard card, bool explicitPreview)
    {
        if (card.Movie is not { } movie || !card.IsLoaded || OverlayLayer.ActualWidth < 100 || OverlayLayer.ActualHeight < 100)
            return;

        Close(clearAmbient: false);
        _active = card;
        _explicitPreview = explicitPreview;
        PreviewPanel.DataContext = movie;
        var soon = movie.Status == MovieStatus.ComingSoon;
        var primaryCommand = soon ? card.RemindCommand : BookMovieCommand;
        PrimaryAction.Content = soon ? "Nhắc tôi" : "Đặt vé ngay";
        PrimaryAction.Visibility = primaryCommand?.CanExecute(movie) == true ? Visibility.Visible : Visibility.Collapsed;
        PreviewVideo.Prepare(movie, preview: true);
        Ambient.ShowMovie(movie);
        PreviewGlow.ShowMovie(movie);
        PositionPanel(card);

        PreviewPanel.Opacity = 1;
        PreviewPanel.IsHitTestVisible = true;
        PreviewPanel.Visibility = Visibility.Visible;
        PreviewPanel.UpdateLayout();
        if (SystemParameters.ClientAreaAnimation)
        {
            AnimateOpen();
            PreviewVideo.Play();
        }

        if (explicitPreview) DetailAction.Focus();
        else ScheduleClose();
    }

    private void PositionPanel(MoviePosterCard card)
    {
        const double padding = 8;
        var viewportWidth = OverlayLayer.ActualWidth;
        var viewportHeight = OverlayLayer.ActualHeight;
        var width = Math.Min(348, viewportWidth - padding * 2);
        PreviewPanel.Width = width;
        PreviewPanel.MaxHeight = viewportHeight - padding * 2;
        PreviewVideo.Height = Math.Min((width - 24) * 9 / 16, viewportHeight * .43);

        PreviewPanel.Visibility = Visibility.Visible;
        PreviewPanel.Measure(new Size(width, PreviewPanel.MaxHeight));
        var anchor = card.PosterAnchor.TranslatePoint(new Point(), OverlayLayer);
        var left = anchor.X + (card.PosterAnchor.ActualWidth - width) / 2;
        var top = anchor.Y + 5;
        Canvas.SetLeft(PreviewPanel, Math.Clamp(left, padding, Math.Max(padding, viewportWidth - width - padding)));
        Canvas.SetTop(PreviewPanel, Math.Clamp(top, padding, Math.Max(padding, viewportHeight - PreviewPanel.DesiredSize.Height - padding)));
    }

    private void AnimateOpen()
    {
        var duration = TimeSpan.FromMilliseconds(300);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        PreviewPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(.60, 1, duration)
        {
            EasingFunction = ease,
            FillBehavior = FillBehavior.Stop,
        });
        var scale = new DoubleAnimation(.90, 1, duration) { EasingFunction = ease, FillBehavior = FillBehavior.Stop };
        PreviewScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        PreviewScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    private void CancelOpening()
    {
        _openTimer.Stop();
        _pending = null;
    }

    private void ScheduleClose()
    {
        if (_active is null || _explicitPreview) return;
        _closeTimer.Stop();
        _closeTimer.Start();
    }

    private void Close(bool clearAmbient = true)
    {
        _closeTimer.Stop();
        if (_active is null) return;
        _active = null;
        _explicitPreview = false;
        PreviewVideo.Release();
        PreviewPanel.BeginAnimation(OpacityProperty, null);
        PreviewScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        PreviewScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        PreviewPanel.Visibility = Visibility.Collapsed;
        PreviewPanel.IsHitTestVisible = false;
        PreviewPanel.DataContext = null;
        if (clearAmbient) Ambient.ShowMovie(null);
    }

    private void Dismiss()
    {
        CancelOpening();
        Close();
    }

    private void OnPanelEnter(object sender, MouseEventArgs e)
    {
        CancelOpening();
        _closeTimer.Stop();
    }

    private void OnPanelLeave(object sender, MouseEventArgs e) => ScheduleClose();

    private void OnPanelFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue) _closeTimer.Stop();
        else ScheduleClose();
    }

    private void OnGalleryScroll(object sender, ScrollChangedEventArgs e)
    {
        if (e.OriginalSource is Visual source && (source == PreviewPanel || PreviewPanel.IsAncestorOf(source))) return;
        if (e.VerticalChange != 0 || e.HorizontalChange != 0) Dismiss();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_active is not null && e.OriginalSource is Visual source && source != PreviewPanel && !PreviewPanel.IsAncestorOf(source))
            Dismiss();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _active is not { } card) return;
        Dismiss();
        card.Focus();
        e.Handled = true;
    }

    private void OnPrimaryAction(object sender, RoutedEventArgs e)
    {
        if (_active is not { Movie: { } movie } card) return;
        var command = movie.Status == MovieStatus.ComingSoon ? card.RemindCommand : BookMovieCommand;
        if (command?.CanExecute(movie) != true) return;
        var restoreFocus = _explicitPreview && movie.Status == MovieStatus.ComingSoon;
        Dismiss();
        command.Execute(movie);
        if (restoreFocus) card.Focus();
    }

    private void OnDetailAction(object sender, RoutedEventArgs e)
    {
        if (_active is not { } card) return;
        Dismiss();
        card.OpenDetails();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _window = Window.GetWindow(this);
        if (_window is not null) _window.Deactivated += OnWindowDeactivated;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_window is not null) _window.Deactivated -= OnWindowDeactivated;
        _window = null;
        Dismiss();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => Dismiss();
}
