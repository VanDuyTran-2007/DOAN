using System.Globalization;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>Trang Đăng nhập / Đăng ký (full cửa sổ, ẩn header).</summary>
public partial class AuthViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IToastService _toast;
    private readonly IMovieService _movies;

    public AuthViewModel(IAuthService auth, INavigationService nav, IToastService toast, IMovieService movies)
    {
        _auth = auth; _nav = nav; _toast = toast; _movies = movies;
        SignIn.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SignIn.CanSubmit)) SignInCommand.NotifyCanExecuteChanged();
        };
        SignUp.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SignUp.CanSubmit)) SignUpCommand.NotifyCanExecuteChanged();
        };
    }

    public SignInFormViewModel SignIn { get; } = new();
    public SignUpFormViewModel SignUp { get; } = new();
    /// <summary>Phim dùng cho collage nền.</summary>
    [ObservableProperty] private IReadOnlyList<Movie> _wallMovies = [];

    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private string? _serverError;

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        SelectedTab = parameter is AuthTab.SignUp ? 1 : 0;
        try { WallMovies = await _movies.GetNowShowingAsync(); }
        catch { /* Không có phim: nền chỉ còn gradient */ }
    }

    partial void OnSelectedTabChanged(int value) => ServerError = null;

    private bool CanSignIn() => SignIn.CanSubmit;
    private bool CanSignUp() => SignUp.CanSubmit;

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        ServerError = null;
        if (!SignIn.Validate()) return;
        IsBusy = true;
        try
        {
            var r = await _auth.SignInAsync(SignIn.Identity.Trim(), SignIn.Password);
            if (!r.Success) { ServerError = r.Error; return; }
            _toast.Show($"Xin chào, {r.User?.FullName}!", ToastKind.Success);
            LeaveAfterSuccess();
        }
        catch (Exception ex) { ServerError = "Không kết nối được máy chủ: " + ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanSignUp))]
    private async Task SignUpAsync()
    {
        ServerError = null;
        if (!SignUp.Validate()) return;
        IsBusy = true;
        try
        {
            var birth = DateTime.ParseExact(SignUp.BirthDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var r = await _auth.SignUpAsync(new SignUpRequest(SignUp.FullName.Trim(), SignUp.Email.Trim(), SignUp.Phone.Trim(), birth, SignUp.Password));
            if (!r.Success) { ServerError = r.Error; return; }
            _toast.Show("Đăng ký thành công, chào mừng bạn đến CineBook!", ToastKind.Success);
            LeaveAfterSuccess();
        }
        catch (Exception ex) { ServerError = "Không kết nối được máy chủ: " + ex.Message; }
        finally { IsBusy = false; }
    }

    private void LeaveAfterSuccess()
    {
        if (_nav.CanGoBack) _nav.GoBack(); else _nav.NavigateTo<HomeViewModel>();
    }

    [RelayCommand] private void BackHome() => _nav.NavigateTo<HomeViewModel>();
    [RelayCommand] private void GoSignUp() => SelectedTab = 1;
    [RelayCommand] private void GoSignIn() => SelectedTab = 0;

    [RelayCommand]
    private void Social(string provider)
        // Chỉ là UI. TODO-BACKEND: POST /api/auth/oauth/{provider} (OAuth2 code flow) → AuthResult.
        => _toast.Show($"Đăng nhập bằng {provider} chưa được hỗ trợ trong bản demo", ToastKind.Info);

    [RelayCommand]
    private void ForgotPassword()
        // TODO-BACKEND: POST /api/auth/forgot-password body {identity} → 204 (gửi OTP/email đặt lại)
        => _toast.Show("Chức năng quên mật khẩu sẽ có khi backend sẵn sàng", ToastKind.Info);
}
