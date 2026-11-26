using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CineBook.ViewModels;

namespace CineBook.Controls;

public partial class ShowtimeGroupsView : UserControl
{
    public ShowtimeGroupsView() => InitializeComponent();

    public static readonly DependencyProperty GroupsProperty = DependencyProperty.Register(nameof(Groups), typeof(IReadOnlyList<ShowtimeGroup>), typeof(ShowtimeGroupsView));
    public static readonly DependencyProperty BookCommandProperty = DependencyProperty.Register(nameof(BookCommand), typeof(ICommand), typeof(ShowtimeGroupsView));

    public IReadOnlyList<ShowtimeGroup>? Groups { get => (IReadOnlyList<ShowtimeGroup>?)GetValue(GroupsProperty); set => SetValue(GroupsProperty, value); }
    /// <summary>Đặt vé cho suất chiếu. Tham số = Showtime.</summary>
    public ICommand? BookCommand { get => (ICommand?)GetValue(BookCommandProperty); set => SetValue(BookCommandProperty, value); }
}
