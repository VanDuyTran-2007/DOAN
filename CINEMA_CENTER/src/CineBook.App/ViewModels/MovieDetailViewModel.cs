using System.Collections.ObjectModel;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>Chi tiết phim + lịch chiếu: chọn ngày → lọc hãng/thành phố → danh sách rạp và giờ chiếu theo định dạng.</summary>
public partial class MovieDetailViewModel : ViewModelBase
{
    private readonly IMovieService _movies;
    private readonly ICinemaService _cinemas;
    private readonly IShowtimeService _showtimes;
    private readonly INavigationService _nav;
    private readonly IToastService _toast;
    private readonly AppState _state;
    private bool _ready;

    public MovieDetailViewModel(IMovieService movies, ICinemaService cinemas, IShowtimeService showtimes,
                                INavigationService nav, IToastService toast, AppState state)
    {
        _movies = movies; _cinemas = cinemas; _showtimes = showtimes; _nav = nav; _toast = toast; _state = state;
        Schedule = new SectionState<CinemaShowtimes>(LoadScheduleAsync);
        _state.PropertyChanged += OnStateChanged;
    }

    public SectionState<CinemaShowtimes> Schedule { get; }
    public ObservableCollection<DateOption> Dates { get; } = [];
    public ObservableCollection<BrandFilter> BrandFilters { get; } = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsComingSoon))] private Movie? _movie;
    [ObservableProperty] private bool _notFound;
    [ObservableProperty] private string? _loadError;

    public bool IsComingSoon => Movie?.Status == MovieStatus.ComingSoon;
    public string CityText => _state.SelectedCity?.City ?? "";
    internal bool OpenSchedule { get; private set; }
    internal bool PlayTrailerOnOpen { get; private set; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        var args = parameter as MovieDetailArgs;
        var movieId = args?.MovieId ?? "";
        OpenSchedule = args?.OpenSchedule == true;
        PlayTrailerOnOpen = args?.PlayTrailerOnOpen == true;
        IsBusy = true;
        try
        {
            Movie = await _movies.GetByIdAsync(movieId);
            NotFound = Movie is null;
            if (Movie is null) return;

            // Thanh chọn 7 ngày: mỗi lần chọn là nạp lại lịch chiếu.
            foreach (var d in DateOption.NextDays())
            {
                d.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(DateOption.IsSelected) && d.IsSelected && _ready) _ = Schedule.LoadAsync(); };
                Dates.Add(d);
            }
            BrandFilters.Add(NewFilter(null, "Tất cả hãng", true));
            foreach (var b in await _cinemas.GetBrandsAsync()) BrandFilters.Add(NewFilter(b.Id, b.Name, false));

            Dates[0].IsSelected = true;
            _ready = true;
            if (!IsComingSoon) await Schedule.LoadAsync();
        }
        catch (Exception ex) { LoadError = ex.Message; }
        finally { IsBusy = false; }
    }

    public override void OnNavigatedFrom()
    {
        _state.PropertyChanged -= OnStateChanged;
        Schedule.Cancel();
    }

    private BrandFilter NewFilter(string? id, string name, bool selected)
    {
        var f = new BrandFilter { Id = id, Name = name, IsSelected = selected };
        f.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BrandFilter.IsSelected) && f.IsSelected && _ready) _ = Schedule.LoadAsync(); };
        return f;
    }

    private void OnStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppState.SelectedCity) || !_ready) return;
        OnPropertyChanged(nameof(CityText));
        if (!IsComingSoon) _ = Schedule.LoadAsync();
    }

    private async Task<IReadOnlyList<CinemaShowtimes>> LoadScheduleAsync(CancellationToken ct)
    {
        if (Movie is null) return [];
        var date = Dates.FirstOrDefault(d => d.IsSelected)?.Date ?? DateTime.Today;
        var brandId = BrandFilters.FirstOrDefault(b => b.IsSelected)?.Id;

        // TODO-BACKEND: GET /api/showtimes?movieId=&date= → List<Showtime>; GET /api/cinemas?brandId=&city= → List<Cinema>
        // (backend có thể gộp thành 1 endpoint trả sẵn nhóm theo rạp để giảm số lần gọi)
        var showtimes = await _showtimes.GetShowtimesAsync(Movie.Id, null, date, ct);
        var cinemas = await _cinemas.GetCinemasAsync(new CinemaFilter(brandId, _state.SelectedCity?.City), ct);
        return cinemas.Select(c => new CinemaShowtimes { Cinema = c, Groups = ShowtimeGroup.Build(showtimes.Where(s => s.CinemaId == c.Id)) })
                      .Where(x => x.Groups.Count > 0).ToList();
    }

    [RelayCommand]
    private void Book(Showtime showtime) => _nav.NavigateTo<BookingViewModel>(new BookingArgs(showtime.Id));

    [RelayCommand]
    private void Remind()
        // TODO-BACKEND: POST /api/me/reminders body {movieId}
        => _toast.Show($"Đã bật nhắc lịch khởi chiếu \"{Movie?.Title}\"", ToastKind.Success);

    [RelayCommand] private void GoBack() { if (_nav.CanGoBack) _nav.GoBack(); else _nav.NavigateTo<HomeViewModel>(); }
    [RelayCommand] private Task RetrySchedule() => Schedule.LoadAsync();
}
