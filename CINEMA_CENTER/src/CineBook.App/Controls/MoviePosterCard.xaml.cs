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
        DataContextChanged += (_, _) => Refresh();
    }

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(MoviePosterCard));
    public static readonly DependencyProperty RemindCommandProperty = DependencyProperty.Register(nameof(RemindCommand), typeof(ICommand), typeof(MoviePosterCard));

    /// <summary>Mở chi tiết/đặt vé. Tham số = Movie.</summary>
    public ICommand? OpenCommand { get => (ICommand?)GetValue(OpenCommandProperty); set => SetValue(OpenCommandProperty, value); }
    /// <summary>"Nhắc tôi" cho phim sắp chiếu. Tham số = Movie.</summary>
    public ICommand? RemindCommand { get => (ICommand?)GetValue(RemindCommandProperty); set => SetValue(RemindCommandProperty, value); }

    private Movie? Movie => DataContext as Movie;

    private void Refresh()
    {
        if (Movie is not { } m) return;
        bool soon = m.Status == MovieStatus.ComingSoon;
        ActionButton.Content = soon ? "Nhắc tôi" : "Đặt vé";
        ReleaseText.Visibility = soon ? Visibility.Visible : Visibility.Collapsed;
        ReleaseText.Text = "Khởi chiếu " + m.ReleaseText;
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (Movie is { } m && OpenCommand?.CanExecute(m) == true) OpenCommand.Execute(m);
    }

    private void OnActionClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // không để click lan lên card
        if (Movie is not { } m) return;
        var cmd = m.Status == MovieStatus.ComingSoon ? RemindCommand : OpenCommand;
        if (cmd?.CanExecute(m) == true) cmd.Execute(m);
    }
}
