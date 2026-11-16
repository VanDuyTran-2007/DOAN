using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CineBook.Controls;

public partial class CinemaCard : UserControl
{
    public CinemaCard() => InitializeComponent();

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(CinemaCard));

    /// <summary>Mở lịch chiếu của rạp. Tham số = Cinema.</summary>
    public ICommand? OpenCommand { get => (ICommand?)GetValue(OpenCommandProperty); set => SetValue(OpenCommandProperty, value); }
}
