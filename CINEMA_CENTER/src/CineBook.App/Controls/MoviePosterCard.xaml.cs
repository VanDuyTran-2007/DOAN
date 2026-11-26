using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CineBook.Models;

namespace CineBook.Controls;

/// <summary>
/// Card poster phim. Code-behind chỉ chuyển sự kiện click thành Command (thuần UI),
/// logic điều hướng nằm ở ViewModel truyền vào qua OpenCommand/RemindCommand.
/// </summary>
public partial class MoviePosterCard : UserControl
{
    public MoviePosterCard()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            _previewHost?.Forget(this);
            Refresh();
        };
        Loaded += (_, _) => _previewHost = CinematicPreviewHost.FindFor(this);
        Unloaded += (_, _) =>
        {
            _previewHost?.Forget(this);
            _previewHost = null;
        };
        MouseEnter += (_, _) => _previewHost?.PointerEntered(this);
        MouseLeave += (_, _) => _previewHost?.PointerLeft(this);
    }

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(MoviePosterCard));
    public static readonly DependencyProperty RemindCommandProperty = DependencyProperty.Register(nameof(RemindCommand), typeof(ICommand), typeof(MoviePosterCard));

    /// <summary>Mở chi tiết/đặt vé. Tham số = Movie.</summary>
    public ICommand? OpenCommand { get => (ICommand?)GetValue(OpenCommandProperty); set => SetValue(OpenCommandProperty, value); }
    /// <summary>"Nhắc tôi" cho phim sắp chiếu. Tham số = Movie.</summary>
    public ICommand? RemindCommand { get => (ICommand?)GetValue(RemindCommandProperty); set => SetValue(RemindCommandProperty, value); }

    private CinematicPreviewHost? _previewHost;

    internal Movie? Movie => DataContext as Movie;

    private void Refresh()
    {
        if (Movie is not { } m) return;
        bool soon = m.Status == MovieStatus.ComingSoon;
        ReleaseText.Visibility = soon ? Visibility.Visible : Visibility.Collapsed;
        ReleaseText.Text = "Khởi chiếu " + m.ReleaseText;
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice?.TabletDevice.Type == TabletDeviceType.Touch && _previewHost is not null)
            _previewHost.ShowForInput(this);
        else
            OpenDetails();
        e.Handled = true;
    }

    protected override void OnTouchUp(TouchEventArgs e)
    {
        base.OnTouchUp(e);
        if (_previewHost is null) return;
        _previewHost.ShowForInput(this);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var menuKey = e.Key == Key.Apps || (e.Key == Key.F10 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
        if (_previewHost is not null && (menuKey || e.Key == Key.Space))
        {
            _previewHost.ShowForInput(this);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            OpenDetails();
            e.Handled = true;
        }
    }

    internal void OpenDetails()
    {
        if (Movie is { } movie && OpenCommand?.CanExecute(movie) == true)
            OpenCommand.Execute(movie);
    }
}
