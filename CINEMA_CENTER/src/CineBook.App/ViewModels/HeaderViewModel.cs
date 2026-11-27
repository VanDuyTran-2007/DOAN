using System.Collections.ObjectModel;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>ViewModel của thanh Header (menu, tìm kiếm, vị trí, tài khoản).</summary>
public partial class HeaderViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IMovieService _movies;
    private readonly IAuthService _auth;
    private readonly IToastService _toast;
    private CancellationTokenSource? _searchCts;

    public HeaderViewModel(INavigationService nav, IMovieService movies, IAuthService auth, ILocationService location,
                           AppState state, IToastService toast)
    {
        _nav = nav; _movies = movies; _auth = auth; _toast = toast;
        State = state;
        Cities = location.Cities;
        State.SelectedCity ??= Cities[0];

        _auth.AuthStateChanged += (_, _) => { OnPropertyChanged(nameof(IsSignedIn)); OnPropertyChanged(nameof(CurrentUser)); };
        _nav.Navigated += OnNavigated;
        _ = LoadGenresAsync(movies);
    }

    public AppState State { get; }
    public IReadOnlyList<GeoLocation> Cities { get; }
    public ObservableCollection<string> Genres { get; } = [];
    public ObservableCollection<Movie> SearchResults { get; } = [];

    public bool IsSignedIn => _auth.CurrentUser is not null;
    public User? CurrentUser => _auth.CurrentUser;

    /// <summary>Khoá mục menu đang active: Booking | Cinemas | Categories | Movies | "" (trang chủ).</summary>
    [ObservableProperty] private string _activeMenu = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isSearchOpen;
    [ObservableProperty] private bool _isCategoryOpen;
    [ObservableProperty] private bool _isUserMenuOpen;

    private async Task LoadGenresAsync(IMovieService movies)
    {
        try { foreach (var g in await movies.GetGenresAsync()) Genres.Add(g); }
        catch { /* Danh mục không tải được: dropdown rỗng, không chặn app */ }
    }

    private void OnNavigated(object? sender, EventArgs e)
    {
        // Rời các trang thuộc menu thì bỏ gạch chân.
        if (_nav.Current is HomeViewModel or MovieDetailViewModel or AuthViewModel or BookingViewModel or MyTicketsViewModel or AccountViewModel)
            ActiveMenu = "";
        else if (_nav.Current is CinemasViewModel) ActiveMenu = "Cinemas";
    }

    // ---- Tìm kiếm có debounce 300ms ----
    partial void OnSearchTextChanged(string value) => _ = SearchAsync(value);

    private async Task SearchAsync(string keyword)
    {
        _searchCts?.Cancel();
        if (string.IsNullOrWhiteSpace(keyword)) { SearchResults.Clear(); IsSearchOpen = false; return; }
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;
        try
        {
            await Task.Delay(300, ct);
            var found = await _movies.SearchAsync(keyword.Trim(), ct);
            SearchResults.Clear();
            foreach (var m in found.Take(6)) SearchResults.Add(m);
            IsSearchOpen = true;
        }
        catch (OperationCanceledException) { }
        catch { _toast.Show("Không tìm kiếm được, vui lòng thử lại", ToastKind.Error); }
    }

    [RelayCommand]
    private void OpenResult(Movie movie)
    {
        IsSearchOpen = false;
        SearchText = "";
        _nav.NavigateTo<MovieDetailViewModel>(new MovieDetailArgs(movie.Id));
    }

    // ---- Điều hướng ----
    [RelayCommand] private void GoHome() { ActiveMenu = ""; _nav.NavigateTo<HomeViewModel>(); }
    [RelayCommand] private void GoBooking() { ActiveMenu = "Booking"; _nav.NavigateTo<CategoriesViewModel>(new CategoriesArgs(null, MovieStatus.NowShowing)); }
    [RelayCommand] private void GoCinemas() { ActiveMenu = "Cinemas"; _nav.NavigateTo<CinemasViewModel>(new CinemasArgs()); }
    [RelayCommand] private void GoMovies() { ActiveMenu = "Movies"; _nav.NavigateTo<CategoriesViewModel>(new CategoriesArgs()); }
    [RelayCommand] private void ToggleCategory() => IsCategoryOpen = !IsCategoryOpen;

    [RelayCommand]
    private void GoGenre(string genre)
    {
        IsCategoryOpen = false;
        ActiveMenu = "Categories";
        _nav.NavigateTo<CategoriesViewModel>(new CategoriesArgs(genre));
    }

    [RelayCommand] private void SignIn() => _nav.NavigateTo<AuthViewModel>(AuthTab.SignIn);
    [RelayCommand] private void SignUp() => _nav.NavigateTo<AuthViewModel>(AuthTab.SignUp);
    [RelayCommand] private void ToggleUserMenu() => IsUserMenuOpen = !IsUserMenuOpen;

    [RelayCommand]
    private void GoAccount()
    {
        IsUserMenuOpen = false;
        if (IsSignedIn) _nav.NavigateTo<AccountViewModel>();
        else _nav.NavigateTo<AuthViewModel>(AuthTab.SignIn);
    }

    [RelayCommand]
    private void GoMyTickets() { IsUserMenuOpen = false; _nav.NavigateTo<MyTicketsViewModel>(); }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        IsUserMenuOpen = false;
        await _auth.SignOutAsync();
        _toast.Show("Đã đăng xuất", ToastKind.Success);
        _nav.NavigateTo<HomeViewModel>();
    }
}
