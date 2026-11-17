using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CineBook.Controls;

/// <summary>
/// Sơ đồ ghế. Code-behind chỉ lo zoom (thuần UI); logic chọn ghế nằm ở BookingViewModel qua SeatCommand.
/// </summary>
public partial class SeatMapControl : UserControl
{
    private const double MinZoom = 0.6, MaxZoom = 1.8, Step = 0.1;

    public SeatMapControl() => InitializeComponent();

    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(IEnumerable), typeof(SeatMapControl));
    public static readonly DependencyProperty SeatCommandProperty = DependencyProperty.Register(nameof(SeatCommand), typeof(ICommand), typeof(SeatMapControl));
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(SeatMapControl), new PropertyMetadata(1.0));

    public IEnumerable? Rows { get => (IEnumerable?)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }
    /// <summary>Lệnh khi bấm một ghế. Tham số = SeatItemViewModel.</summary>
    public ICommand? SeatCommand { get => (ICommand?)GetValue(SeatCommandProperty); set => SetValue(SeatCommandProperty, value); }
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, Math.Clamp(value, MinZoom, MaxZoom)); }

    private void OnZoomIn(object sender, RoutedEventArgs e) => Zoom = Math.Round(Zoom + Step, 2);
    private void OnZoomOut(object sender, RoutedEventArgs e) => Zoom = Math.Round(Zoom - Step, 2);

    // Ctrl + lăn chuột = zoom; lăn chuột thường vẫn cuộn.
    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        Zoom = Math.Round(Zoom + (e.Delta > 0 ? Step : -Step), 2);
        e.Handled = true;
    }
}
