using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace CineBook.Controls;

public partial class DateStrip : UserControl
{
    public DateStrip() => InitializeComponent();

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(DateStrip));
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
}
