using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CineBook.Models;
using CineBook.ViewModels;

namespace CineBook.Controls;

public partial class MovieShelf : UserControl
{
    private ScrollViewer? _scroller;

    public MovieShelf() => InitializeComponent();

    /// <summary>6 ô skeleton cho trạng thái loading.</summary>
    public static readonly int[] SkeletonSlots = [1, 2, 3, 4, 5, 6];

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(MovieShelf));
    public static readonly DependencyProperty SectionProperty = DependencyProperty.Register(nameof(Section), typeof(SectionState<Movie>), typeof(MovieShelf));
    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(MovieShelf));
    public static readonly DependencyProperty RemindCommandProperty = DependencyProperty.Register(nameof(RemindCommand), typeof(ICommand), typeof(MovieShelf));
    public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(MovieShelf));

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public SectionState<Movie>? Section { get => (SectionState<Movie>?)GetValue(SectionProperty); set => SetValue(SectionProperty, value); }
    public ICommand? OpenCommand { get => (ICommand?)GetValue(OpenCommandProperty); set => SetValue(OpenCommandProperty, value); }
    public ICommand? RemindCommand { get => (ICommand?)GetValue(RemindCommandProperty); set => SetValue(RemindCommandProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    private void OnScrollerLoaded(object sender, RoutedEventArgs e)
    {
        if (_scroller is not null) return;
        _scroller = (ScrollViewer)sender;
        // ScrollViewer ngang nuốt sự kiện lăn chuột → chuyển lên cha để trang vẫn cuộn dọc được.
        _scroller.PreviewMouseWheel += (s, args) =>
        {
            args.Handled = true;
            var forward = new MouseWheelEventArgs(args.MouseDevice, args.Timestamp, args.Delta) { RoutedEvent = MouseWheelEvent, Source = s };
            (Parent as UIElement)?.RaiseEvent(forward);
        };
    }

    // Cuộn ngang theo bước ~ 3 card (thuần UI).
    private void OnScrollLeft(object sender, RoutedEventArgs e) => _scroller?.ScrollToHorizontalOffset(_scroller.HorizontalOffset - 540);
    private void OnScrollRight(object sender, RoutedEventArgs e) => _scroller?.ScrollToHorizontalOffset(_scroller.HorizontalOffset + 540);
}
