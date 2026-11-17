using System.Collections.ObjectModel;
using System.Windows.Threading;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>Chấm điều hướng của carousel.</summary>
public partial class HeroDot : ObservableObject
{
    public int Index { get; init; }
    [ObservableProperty] private bool _isActive;
}

/// <summary>Chip lọc theo hãng rạp (Id = null nghĩa là "Tất cả").</summary>
public partial class BrandFilter : ObservableObject
{
    public string? Id { get; init; }
    public required string Name { get; init; }
    [ObservableProperty] private bool _isSelected;
}

public partial class HomeViewModel : ViewModelBase
{
    private readonly IMovieService _movies;
    private readonly ICinemaService _cinemas;
    private readonly ILocationService _location;
    private readonly INavigationService _nav;
    private readonly IToastService _toast;
    private readonly AppState _state;
    private readonly DispatcherTimer _heroTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private IReadOnlyList<Cinema>? _nearbyCache;
    private bool _suppressCityReload;

    public HomeViewModel(IMovieService movies, ICinemaService cinemas, ILocationService location,
                         INavigationService nav, IToastService toast, AppState state)
    {
        _movies = movies; _cinemas = cinemas; _location = location; _nav = nav; _toast = toast; _state = state;

        NowShowing = new SectionState<Movie>(ct => movies.GetNowShowingAsync(ct));
        Hot = new SectionState<Movie>(ct => movies.GetHotAsync(ct));
        ComingSoon = new SectionState<Movie>(ct => movies.GetComingSoonAsync(ct));
        Nearby = new SectionState<Cinema>(LoadNearbyAsync);

        _heroTimer.Tick += (_, _) => NextHero();
        _state.PropertyChanged += OnStateChanged;
    }

    public SectionState<Movie> NowShowing { get; }
    public SectionState<Movie> Hot { get; }
    public SectionState<Movie> ComingSoon { get; }
    public SectionState<Cinema> Nearby { get; }

    public ObservableCollection<Movie> HeroMovies { get; } = [];
    public ObservableCollection<HeroDot> HeroDots { get; } = [];
    public ObservableCollection<BrandFilter> BrandFilters { get; } = [];

    [ObservableProperty] private Movie? _currentHero;
    [ObservableProperty] private string _locationText = "Đang xác định vị trí...";
    private int _heroIndex;

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await Task.WhenAll(LoadHeroAsync(), NowShowing.LoadAsync(), Hot.LoadAsync(), ComingSoon.LoadAsync(), InitNearbyAsync());
    }

    public override void OnNavigatedFrom()
    {
        _heroTimer.Stop();
        _state.PropertyChanged -= OnStateChanged;
        NowShowing.Cancel(); Hot.Cancel(); ComingSoon.Cancel(); Nearby.Cancel();
    }

    // ---------- Hero carousel ----------
    private async Task LoadHeroAsync()
    {
        try
        {
            var hot = await _movies.GetHotAsync();
            foreach (var m in hot.Where(m => m.Status == MovieStatus.NowShowing).Take(4)) HeroMovies.Add(m);
            for (int i = 0; i < HeroMovies.Count; i++) HeroDots.Add(new HeroDot { Index = i });
            ShowHero(0);
            _heroTimer.Start(); // tự chuyển slide sau 6 giây
        }
        catch { /* Banner lỗi: bỏ qua, các section bên dưới vẫn có nút Thử lại */ }
    }

    private void ShowHero(int index)
    {
        if (HeroMovies.Count == 0) return;
        _heroIndex = (index % HeroMovies.Count + HeroMovies.Count) % HeroMovies.Count;
        CurrentHero = HeroMovies[_heroIndex];
        foreach (var d in HeroDots) d.IsActive = d.Index == _heroIndex;
    }

    private void NextHero() => ShowHero(_heroIndex + 1);

    [RelayCommand]
    private void SelectHero(int index)
    {
        ShowHero(index);
        _heroTimer.Stop(); _heroTimer.Start(); // người dùng chủ động chọn → reset đếm 6 giây
    }

    // ---------- Rạp gần bạn ----------
    private async Task InitNearbyAsync()
    {
        try
        {
            var brands = await _cinemas.GetBrandsAsync();
            BrandFilters.Add(NewFilter(null, "Tất cả", true));
            foreach (var b in brands) BrandFilters.Add(NewFilter(b.Id, b.Name, false));

            // Lấy vị trí hiện tại; nếu bị từ chối (null) thì dùng thành phố chọn thủ công ở Header.
            var loc = await _location.GetCurrentLocationAsync();
            if (loc is not null)
            {
                _suppressCityReload = true;
                _state.SelectedCity = _location.Cities.FirstOrDefault(c => c.City == loc.City) ?? loc;
                _suppressCityReload = false;
                LocationText = $"Vị trí của bạn: {loc.City}";
            }
            else LocationText = "Chưa có quyền vị trí – hãy chọn thành phố ở góc trên bên phải";
        }
        catch { LocationText = "Không xác định được vị trí – hãy chọn thành phố thủ công"; }
        await Nearby.LoadAsync();
    }

    private BrandFilter NewFilter(string? id, string name, bool selected)
    {
        var f = new BrandFilter { Id = id, Name = name, IsSelected = selected };
        f.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BrandFilter.IsSelected) && f.IsSelected) _ = Nearby.LoadAsync();
        };
        return f;
    }

    // Thành phố đổi ở Header → nạp lại rạp gần bạn.
    private void OnStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppState.SelectedCity) || _suppressCityReload) return;
        _nearbyCache = null;
        LocationText = $"Thành phố: {_state.SelectedCity?.City}";
        _ = Nearby.LoadAsync();
    }

    private async Task<IReadOnlyList<Cinema>> LoadNearbyAsync(CancellationToken ct)
    {
        // TODO-BACKEND: GET /api/cinemas/nearby?lat=&lng=&radiusKm= → List<Cinema> (đã có DistanceKm)
        if (_nearbyCache is null)
        {
            var city = _state.SelectedCity ?? _location.Cities[0];
            _nearbyCache = await _cinemas.GetNearbyAsync(city.Latitude, city.Longitude, 40, ct);
        }
        var brandId = BrandFilters.FirstOrDefault(b => b.IsSelected)?.Id;
        return _nearbyCache.Where(c => brandId is null || c.BrandId == brandId).ToList();
    }

    // ---------- Lệnh ----------
    [RelayCommand] private void OpenMovie(Movie movie) => _nav.NavigateTo<MovieDetailViewModel>(new MovieDetailArgs(movie.Id));
    [RelayCommand] private void OpenHero() { if (CurrentHero is { } m) OpenMovie(m); }

    [RelayCommand]
    private void WatchTrailer(Movie? movie)
    {
        movie ??= CurrentHero;
        // TODO-BACKEND: Movie.TrailerUrl (YouTube/CDN). Frontend sẽ nhúng WebView2 khi có URL thật.
        _toast.Show($"Trailer \"{movie?.Title}\" sẽ phát khi có URL thật từ backend", ToastKind.Info);
    }

    [RelayCommand]
    private void Remind(Movie movie)
    {
        // TODO-BACKEND: POST /api/me/reminders body {movieId} → 204. Cần đăng nhập.
        _toast.Show($"Đã bật nhắc lịch khởi chiếu \"{movie.Title}\"", ToastKind.Success);
    }

    [RelayCommand] private void OpenCinema(Cinema cinema) => _nav.NavigateTo<CinemasViewModel>(new CinemasArgs(cinema.Id));

    [RelayCommand] private Task RetryNowShowing() => NowShowing.LoadAsync();
    [RelayCommand] private Task RetryHot() => Hot.LoadAsync();
    [RelayCommand] private Task RetryComingSoon() => ComingSoon.LoadAsync();
    [RelayCommand] private Task RetryNearby() { _nearbyCache = null; return Nearby.LoadAsync(); }
}
