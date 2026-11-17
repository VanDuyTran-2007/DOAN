using System.Collections.ObjectModel;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>Trang Rạp chiếu: bộ lọc (hãng, thành phố, quận) + danh sách cơ sở + phim/lịch chiếu của rạp đang chọn.</summary>
public partial class CinemasViewModel : ViewModelBase
{
    private readonly ICinemaService _cinemas;
    private readonly IShowtimeService _showtimes;
    private readonly INavigationService _nav;
    private readonly AppState _state;
    private bool _suspend = true;
    private string? _pendingCinemaId;

    private static readonly FilterOption<string> AllCities = new("Tất cả thành phố", null);
    private static readonly FilterOption<string> AllDistricts = new("Tất cả quận/huyện", null);

    public CinemasViewModel(ICinemaService cinemas, IShowtimeService showtimes, INavigationService nav, AppState state, ILocationService location)
    {
        _cinemas = cinemas; _showtimes = showtimes; _nav = nav; _state = state;
        CinemaList = new SectionState<Cinema>(LoadCinemasAsync);
        Detail = new SectionState<MovieShowtimes>(LoadDetailAsync);
        Cities = [AllCities, .. location.Cities.Select(c => new FilterOption<string>(c.City, c.City))];
        _selectedCityOption = AllCities;
        _selectedDistrictOption = AllDistricts;
        Districts = [AllDistricts];
    }


    public SectionState<Cinema> CinemaList { get; }
    public SectionState<MovieShowtimes> Detail { get; }
    public ObservableCollection<BrandFilter> BrandFilters { get; } = [];
    public ObservableCollection<FilterOption<string>> Cities { get; }
    public ObservableCollection<FilterOption<string>> Districts { get; }
    public ObservableCollection<DateOption> Dates { get; } = [];

    [ObservableProperty] private FilterOption<string> _selectedCityOption;
    [ObservableProperty] private FilterOption<string> _selectedDistrictOption;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasSelectedCinema))] private Cinema? _selectedCinema;

    public bool HasSelectedCinema => SelectedCinema is not null;

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        _pendingCinemaId = (parameter as CinemasArgs)?.CinemaId;
        try
        {
            BrandFilters.Add(NewFilter(null, "Tất cả hãng", true));
            foreach (var b in await _cinemas.GetBrandsAsync()) BrandFilters.Add(NewFilter(b.Id, b.Name, false));
        }
        catch { /* Không tải được hãng: vẫn xem được danh sách rạp */ }

        foreach (var d in DateOption.NextDays())
        {
            d.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(DateOption.IsSelected) && d.IsSelected && !_suspend) _ = Detail.LoadAsync(); };
            Dates.Add(d);
        }
        Dates[0].IsSelected = true;

        // Vào thẳng một rạp thì bỏ lọc thành phố; còn lại mặc định theo thành phố đang chọn ở header.
        if (_pendingCinemaId is null && _state.SelectedCity is { } c)
            SelectedCityOption = Cities.FirstOrDefault(x => x.Value == c.City) ?? AllCities;

        _suspend = false;
        await CinemaList.LoadAsync();
    }

    public override void OnNavigatedFrom() { CinemaList.Cancel(); Detail.Cancel(); }

    private BrandFilter NewFilter(string? id, string name, bool selected)
    {
        var f = new BrandFilter { Id = id, Name = name, IsSelected = selected };
        f.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BrandFilter.IsSelected) && f.IsSelected && !_suspend) _ = CinemaList.LoadAsync(); };
        return f;
    }

    partial void OnSelectedCityOptionChanged(FilterOption<string> value)
    {
        if (_suspend) return;
        _suspend = true; SelectedDistrictOption = AllDistricts; _suspend = false; // đổi thành phố → bỏ lọc quận
        _ = CinemaList.LoadAsync();
    }

    partial void OnSelectedDistrictOptionChanged(FilterOption<string> value) { if (!_suspend) _ = CinemaList.LoadAsync(); }

    partial void OnSelectedCinemaChanged(Cinema? value) { if (value is not null) _ = Detail.LoadAsync(); else Detail.Items.Clear(); }

    private async Task<IReadOnlyList<Cinema>> LoadCinemasAsync(CancellationToken ct)
    {
        var brandId = BrandFilters.FirstOrDefault(b => b.IsSelected)?.Id;
        // TODO-BACKEND: GET /api/cinemas?brandId=&city=&district= → List<Cinema>
        var all = await _cinemas.GetCinemasAsync(new CinemaFilter(brandId, SelectedCityOption.Value), ct);

        // Danh sách quận suy ra từ kết quả (backend có thể cung cấp GET /api/districts?city=).
        var districts = all.Select(c => c.District).Distinct().OrderBy(d => d).ToList();
        var keep = SelectedDistrictOption.Value;
        _suspend = true;
        Districts.Clear(); Districts.Add(AllDistricts);
        foreach (var d in districts) Districts.Add(new FilterOption<string>(d, d));
        SelectedDistrictOption = Districts.FirstOrDefault(x => x.Value == keep) ?? AllDistricts;
        _suspend = false;

        var result = all.Where(c => SelectedDistrictOption.Value is null || c.District == SelectedDistrictOption.Value).ToList();

        if (_pendingCinemaId is not null)
        {
            SelectedCinema = result.FirstOrDefault(c => c.Id == _pendingCinemaId);
            _pendingCinemaId = null;
        }
        else if (SelectedCinema is not null && result.All(c => c.Id != SelectedCinema.Id)) SelectedCinema = null;
        return result;
    }

    private async Task<IReadOnlyList<MovieShowtimes>> LoadDetailAsync(CancellationToken ct)
    {
        if (SelectedCinema is null) return [];
        var date = Dates.FirstOrDefault(d => d.IsSelected)?.Date ?? DateTime.Today;
        // TODO-BACKEND: GET /api/cinemas/{id}/movies → List<Movie>; GET /api/showtimes?movieId=&cinemaId=&date= → List<Showtime>
        var movies = await _cinemas.GetMoviesAtCinemaAsync(SelectedCinema.Id, ct);
        var rows = await Task.WhenAll(movies.Select(async m =>
        {
            var sts = await _showtimes.GetShowtimesAsync(m.Id, SelectedCinema.Id, date, ct);
            return new MovieShowtimes { Movie = m, Groups = ShowtimeGroup.Build(sts) };
        }));
        return rows.Where(r => r.Groups.Count > 0).ToList();
    }

    [RelayCommand] private void SelectCinema(Cinema cinema) => SelectedCinema = cinema;
    [RelayCommand] private void Book(Showtime showtime) => _nav.NavigateTo<BookingViewModel>(new BookingArgs(showtime.Id));
    [RelayCommand] private void OpenMovie(Movie movie) => _nav.NavigateTo<MovieDetailViewModel>(new MovieDetailArgs(movie.Id));
    [RelayCommand] private Task RetryCinemas() => CinemaList.LoadAsync();
    [RelayCommand] private Task RetryDetail() => Detail.LoadAsync();
}
