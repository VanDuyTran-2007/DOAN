using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CineBook.Models;

namespace CineBook.Controls;

/// <summary>
/// Nền collage poster: nhiều cột poster xếp lưới, nghiêng -8°, mỗi cột trôi chậm vô hạn (cột chẵn lên, cột lẻ xuống).
/// Dựng bằng code vì số cột/hàng phụ thuộc dữ liệu; code-behind này chỉ làm hiển thị + animation.
/// </summary>
public class PosterWall : Grid
{
    private const double PosterWidth = 150, PosterHeight = 225, CellMargin = 8;
    private const int Columns = 12;

    public static readonly DependencyProperty MoviesProperty = DependencyProperty.Register(
        nameof(Movies), typeof(IEnumerable<Movie>), typeof(PosterWall), new PropertyMetadata(null, OnMoviesChanged));

    public IEnumerable<Movie>? Movies { get => (IEnumerable<Movie>?)GetValue(MoviesProperty); set => SetValue(MoviesProperty, value); }

    public PosterWall() => ClipToBounds = true;

    private static void OnMoviesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var wall = (PosterWall)d;
        if (e.OldValue is INotifyCollectionChanged oldC) oldC.CollectionChanged -= wall.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newC) newC.CollectionChanged += wall.OnCollectionChanged;
        wall.Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        Children.Clear();
        var movies = Movies?.ToList() ?? [];
        if (movies.Count == 0) return;

        double cellHeight = PosterHeight + CellMargin * 2;
        double loopHeight = movies.Count * cellHeight;
        // Mỗi cột lặp đủ để phủ màn hình + 1 vòng (để animation nối liền không giật).
        int loops = (int)Math.Ceiling(2600 / loopHeight) + 1;

        var columnsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
        for (int c = 0; c < Columns; c++)
        {
            var col = new StackPanel { Width = PosterWidth + CellMargin * 2 };
            var shift = c * 3 % movies.Count; // lệch từng cột để nhìn không bị lặp hàng ngang
            for (int l = 0; l < loops; l++)
                for (int i = 0; i < movies.Count; i++)
                    col.Children.Add(new PosterView { DataContext = movies[(i + shift) % movies.Count], Width = PosterWidth, Height = PosterHeight, Margin = new Thickness(CellMargin) });

            var move = new TranslateTransform();
            col.RenderTransform = move;
            bool up = c % 2 == 0;
            if (!up) col.Margin = new Thickness(0, -loopHeight, 0, 0); // cột đi xuống: dời lên trước rồi trượt xuống
            var anim = new DoubleAnimation(0, up ? -loopHeight : loopHeight, TimeSpan.FromSeconds(70 + c % 4 * 12))
            { RepeatBehavior = RepeatBehavior.Forever };
            move.BeginAnimation(TranslateTransform.YProperty, anim);
            columnsPanel.Children.Add(col);
        }

        // Nghiêng -8° và đẩy lên để lấp kín các góc sau khi xoay.
        columnsPanel.Margin = new Thickness(0, -300, 0, 0);
        columnsPanel.RenderTransformOrigin = new Point(0.5, 0.5);
        columnsPanel.RenderTransform = new RotateTransform(-8);
        Children.Add(columnsPanel);
    }
}
