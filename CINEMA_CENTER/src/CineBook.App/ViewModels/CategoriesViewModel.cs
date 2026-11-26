using System.Collections.ObjectModel;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>Trang Danh mục / Phim: lưới phim, lọc theo thể loại, trạng thái, định dạng, sắp xếp; "Tải thêm" để phân trang.</summary>
public partial class CategoriesViewModel : ViewModelBase
{
    private const int PageSize = 8;
    private readonly IMovieService _movies;
    private readonly INavigationService _nav;
    private readonly IToastService _toast;
    private bool _suspend = true;
    private int _page = 1;
    private CancellationTokenSource? _cts;

    public CategoriesViewModel(IMovieService movies, INavigationService nav, IToastService toast)
    {
        _movies = movies; _nav = nav; _toast = toast;
        Genres = [new("Tất cả thể loại", null)];
        Statuses = [new("Tất cả", null), new("Đang chiếu", MovieStatus.NowShowing), new("Sắp chiếu", MovieStatus.ComingSoon)];
        Formats = [new("Mọi định dạng", null), new("2D", MovieFormat.TwoD), new("3D", MovieFormat.ThreeD), new("IMAX", MovieFormat.IMAX), new("4DX", MovieFormat.FourDX)];
        Sorts = [new("Mới nhất", MovieSort.Newest), new("Rating cao nhất", MovieSort.Rating)];
        _selectedGenre = Genres[0]; _selectedStatus = Statuses[0]; _selectedFormat = Formats[0]; _selectedSort = Sorts[0];
    }

    public ObservableCollection<FilterOption<string>> Genres { get; }
    public IReadOnlyList<FilterOption<MovieStatus?>> Statuses { get; }
    public IReadOnlyList<FilterOption<MovieFormat?>> Formats { get; }
    public IReadOnlyList<FilterOption<MovieSort?>> Sorts { get; }
    public ObservableCollection<Movie> Items { get; } = [];

    [ObservableProperty] private FilterOption<string> _selectedGenre;
    [ObservableProperty] private FilterOption<MovieStatus?> _selectedStatus;
    [ObservableProperty] private FilterOption<MovieFormat?> _selectedFormat;
    [ObservableProperty] private FilterOption<MovieSort?> _selectedSort;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _hasMore;
    [ObservableProperty] private bool _isLoadingMore;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _title = "Tất cả phim";

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        var args = parameter as CategoriesArgs ?? new CategoriesArgs();
        try { foreach (var g in await _movies.GetGenresAsync()) Genres.Add(new FilterOption<string>(g, g)); } catch { }

        SelectedGenre = Genres.FirstOrDefault(g => g.Value == args.Genre) ?? Genres[0];
        SelectedStatus = Statuses.First(s => s.Value == args.Status);
        _suspend = false;
        await ReloadAsync();
    }

    public override void OnNavigatedFrom() => _cts?.Cancel();

    partial void OnSelectedGenreChanged(FilterOption<string> value) => FilterChanged();
    partial void OnSelectedStatusChanged(FilterOption<MovieStatus?> value) => FilterChanged();
    partial void OnSelectedFormatChanged(FilterOption<MovieFormat?> value) => FilterChanged();
    partial void OnSelectedSortChanged(FilterOption<MovieSort?> value) => FilterChanged();

    private void FilterChanged() { if (!_suspend) _ = ReloadAsync(); }

    private MovieQuery BuildQuery(int page) => new(SelectedGenre.Value, SelectedStatus.Value, SelectedFormat.Value,
                                                   SelectedSort.Value ?? MovieSort.Newest, page, PageSize);

    private async Task ReloadAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _page = 1;
        IsLoading = true; IsError = false; IsEmpty = false;
        Title = SelectedGenre.Value is { } g ? $"Phim {g}" : SelectedStatus.Value == MovieStatus.NowShowing ? "Phim đang chiếu" : "Tất cả phim";
        try
        {
            // TODO-BACKEND: GET /api/movies?genre=&status=&format=&sort=&page=1&pageSize=8 → PagedResult<Movie>
            var r = await _movies.GetByGenreAsync(BuildQuery(1), ct);
            Items.Clear();
            foreach (var m in r.Items) Items.Add(m);
            TotalCount = r.TotalCount; HasMore = r.HasMore;
            IsEmpty = Items.Count == 0;
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex) { Error = ex.Message; IsError = true; }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsLoadingMore || !HasMore) return;
        IsLoadingMore = true;
        try
        {
            var r = await _movies.GetByGenreAsync(BuildQuery(_page + 1), _cts?.Token ?? default);
            _page++;
            foreach (var m in r.Items) Items.Add(m);
            HasMore = r.HasMore;
        }
        catch (Exception) { _toast.Show("Không tải thêm được, thử lại sau", ToastKind.Error); }
        finally { IsLoadingMore = false; }
    }

    [RelayCommand] private void OpenMovie(Movie movie) => _nav.NavigateTo<MovieDetailViewModel>(new MovieDetailArgs(movie.Id));
    [RelayCommand] private void Remind(Movie movie) => _toast.Show($"Đã bật nhắc lịch khởi chiếu \"{movie.Title}\"", ToastKind.Success);
    [RelayCommand] private Task Retry() => ReloadAsync();
}
