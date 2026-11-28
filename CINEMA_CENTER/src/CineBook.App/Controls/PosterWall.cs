using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using CineBook.Converters;
using CineBook.Models;

namespace CineBook.Controls;

/// <summary>Vẽ collage poster trong một lớp nền và tái sử dụng ảnh cho các ô lặp.</summary>
public class PosterWall : FrameworkElement
{
    private const double PosterWidth = 150, PosterHeight = 225, Gutter = 16;
    private const double CellWidth = PosterWidth + Gutter, CellHeight = PosterHeight + Gutter;
    private const double Tilt = -8;
    private static readonly double Angle = Math.Abs(Tilt) * Math.PI / 180;
    private Brush[] _posters = [];
    private INotifyCollectionChanged? _observedMovies;

    public static readonly DependencyProperty MoviesProperty = DependencyProperty.Register(
        nameof(Movies), typeof(IEnumerable<Movie>), typeof(PosterWall),
        new PropertyMetadata(null, (wall, _) =>
        {
            var posterWall = (PosterWall)wall;
            if (posterWall.IsLoaded) posterWall.RefreshMovies();
        }));

    public IEnumerable<Movie>? Movies
    {
        get => (IEnumerable<Movie>?)GetValue(MoviesProperty);
        set => SetValue(MoviesProperty, value);
    }

    public PosterWall()
    {
        ClipToBounds = true;
        IsHitTestVisible = false;
        Loaded += (_, _) => RefreshMovies();
        Unloaded += (_, _) =>
        {
            ObserveMovies(null);
            _posters = [];
            InvalidateVisual();
        };
    }

    private void ObserveMovies(INotifyCollectionChanged? movies)
    {
        if (ReferenceEquals(_observedMovies, movies)) return;
        if (_observedMovies is not null)
            CollectionChangedEventManager.RemoveHandler(_observedMovies, OnCollectionChanged);
        _observedMovies = movies;
        if (_observedMovies is not null)
            CollectionChangedEventManager.AddHandler(_observedMovies, OnCollectionChanged);
    }

    private void RefreshMovies()
    {
        ObserveMovies(Movies as INotifyCollectionChanged);
        _posters = Movies?.Select(movie =>
            PosterBrushConverter.TryCreatePosterBrush(movie.PosterUrl, 224)
                ?? PosterBrushConverter.Gradient(movie.PlaceholderColorHex, new Point(0, 0), new Point(1, 1))).ToArray() ?? [];
        InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshMovies();

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_posters.Length == 0 || ActualWidth <= 0 || ActualHeight <= 0) return;

        double width = ActualWidth * Math.Cos(Angle) + ActualHeight * Math.Sin(Angle);
        double height = ActualHeight * Math.Cos(Angle) + ActualWidth * Math.Sin(Angle);
        int columns = (int)Math.Ceiling(width / CellWidth);
        int rows = (int)Math.Ceiling(height / CellHeight);
        double left = (ActualWidth - columns * CellWidth) / 2;
        double top = (ActualHeight - rows * CellHeight) / 2;

        drawingContext.PushTransform(new RotateTransform(Tilt, ActualWidth / 2, ActualHeight / 2));
        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                var rect = new Rect(left + column * CellWidth + Gutter / 2, top + row * CellHeight + Gutter / 2,
                    PosterWidth, PosterHeight);
                drawingContext.DrawRoundedRectangle(_posters[(row + column * 3) % _posters.Length], null, rect, 8, 8);
            }
        }
        drawingContext.Pop();
    }
}
