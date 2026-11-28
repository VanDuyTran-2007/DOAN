using System.Windows;
using CineBook.ViewModels;

namespace CineBook;

/// <summary>Code-behind chỉ xử lý thao tác cửa sổ (thu nhỏ/phóng to/đóng) – thuần UI.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        StateChanged += OnStateChanged;
        OnStateChanged(this, EventArgs.Empty);
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnMaximize(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnWorkspaceSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0) return;

        const double designWidth = 1280;
        const double designHeight = 684;
        double scale = Math.Min(1, Math.Min(e.NewSize.Width / designWidth, e.NewSize.Height / designHeight));
        WorkspaceScale.ScaleX = scale;
        WorkspaceScale.ScaleY = scale;
    }

    // Cửa sổ không viền khi phóng to sẽ tràn ra ngoài màn hình ~8px → bù lại bằng Margin.
    private void OnStateChanged(object? sender, EventArgs e)
    {
        RootGrid.Margin = WindowState == WindowState.Maximized
            ? new Thickness(SystemParameters.WindowResizeBorderThickness.Left + 2)
            : new Thickness(0);
    }
}
