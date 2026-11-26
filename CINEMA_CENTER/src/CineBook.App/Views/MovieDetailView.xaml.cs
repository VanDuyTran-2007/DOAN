using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CineBook.Models;
using CineBook.ViewModels;

namespace CineBook.Views;

public partial class MovieDetailView : UserControl
{
    private static readonly DependencyProperty AnimatedOffsetProperty = DependencyProperty.Register(
        "AnimatedOffset", typeof(double), typeof(MovieDetailView), new PropertyMetadata(0.0, OnAnimatedOffsetChanged));

    private MovieDetailViewModel? _viewModel;
    private Movie? _displayedMovie;

    public MovieDetailView()
    {
        InitializeComponent();
        Loaded += (_, _) => BindViewModel();
        Unloaded += (_, _) =>
        {
            BindViewModel(active: false);
            BeginAnimation(AnimatedOffsetProperty, null);
        };
        DataContextChanged += (_, _) =>
        {
            if (IsLoaded) BindViewModel();
        };
    }

    private void BindViewModel(bool active = true)
    {
        if (_viewModel is not null) _viewModel.PropertyChanged -= OnMovieChanged;
        _viewModel = active ? DataContext as MovieDetailViewModel : null;
        if (_viewModel is not null) _viewModel.PropertyChanged += OnMovieChanged;
        ApplyMovie();
    }

    private void OnMovieChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MovieDetailViewModel.Movie)) ApplyMovie();
    }

    private void ApplyMovie()
    {
        var movie = _viewModel?.Movie;
        if (ReferenceEquals(movie, _displayedMovie)) return;
        _displayedMovie = movie;
        Ambient.ShowMovie(movie);
        if (movie is null)
        {
            Trailer.Release();
            return;
        }

        Trailer.Prepare(movie);
        if (_viewModel?.PlayTrailerOnOpen == true) Trailer.Play();
        if (_viewModel?.OpenSchedule == true && movie.Status != MovieStatus.ComingSoon)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (IsLoaded && ReferenceEquals(movie, _displayedMovie)) ScrollToSchedule();
            }, DispatcherPriority.Loaded);
        }
    }

    private void OnMediaRowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged || e.NewSize.Width <= 0) return;

        var width = e.NewSize.Width;
        var stacked = width < 560;
        var posterWidth = stacked ? Math.Min(142, width) : Math.Min(260, width * .24);
        const int gap = 20;
        Grid.SetColumnSpan(MoviePoster, stacked ? 2 : 1);
        Grid.SetColumn(Trailer, stacked ? 0 : 1);
        Grid.SetColumnSpan(Trailer, stacked ? 2 : 1);
        Grid.SetRow(Trailer, stacked ? 1 : 0);
        MoviePoster.Width = posterWidth;
        MoviePoster.Height = posterWidth * 1.5;
        MoviePoster.Margin = stacked ? new Thickness(0, 0, 0, gap) : new Thickness(0, 0, gap, 0);
        Trailer.Height = (stacked ? width : width - posterWidth - gap) * 9 / 16;
    }

    private void OnBook(object sender, RoutedEventArgs e) => ScrollToSchedule();

    private void ScrollToSchedule()
    {
        var offset = MovieScroll.VerticalOffset;
        var y = ScheduleHeading.TranslatePoint(new Point(), MovieScroll).Y;
        var destination = Math.Clamp(offset + y - 12, 0, MovieScroll.ScrollableHeight);
        SetValue(AnimatedOffsetProperty, offset);
        BeginAnimation(AnimatedOffsetProperty, null);
        if (!SystemParameters.ClientAreaAnimation || Math.Abs(destination - offset) < 2)
        {
            MovieScroll.ScrollToVerticalOffset(destination);
            return;
        }

        BeginAnimation(AnimatedOffsetProperty, new DoubleAnimation(offset, destination, TimeSpan.FromMilliseconds(330))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });
    }

    private static void OnAnimatedOffsetChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        var view = (MovieDetailView)element;
        view.MovieScroll.ScrollToVerticalOffset((double)e.NewValue);
    }

    private void OnManualScroll(object sender, MouseWheelEventArgs e)
    {
        var offset = MovieScroll.VerticalOffset;
        SetValue(AnimatedOffsetProperty, offset);
        BeginAnimation(AnimatedOffsetProperty, null);
    }
}
