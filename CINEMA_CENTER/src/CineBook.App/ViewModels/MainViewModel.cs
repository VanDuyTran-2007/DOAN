using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CineBook.ViewModels;

/// <summary>ViewModel của cửa sổ chính: Header + trang hiện tại + toast.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IAuthService _auth;

    public MainViewModel(INavigationService nav, HeaderViewModel header, IToastService toast, IAuthService auth)
    {
        _nav = nav;
        _auth = auth;
        Header = header;
        Toast = toast;
        _nav.Navigated += (_, _) =>
        {
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(IsHeaderVisible));
        };
        _auth.AuthStateChanged += (_, _) => OnPropertyChanged(nameof(IsHeaderVisible));
    }

    public HeaderViewModel Header { get; }
    public IToastService Toast { get; }
    public ViewModelBase? Current => _nav.Current;

    /// <summary>Trang đăng nhập/đăng ký ẩn header theo thiết kế.</summary>
    public bool IsHeaderVisible => Current is not AuthViewModel && _auth.CurrentUser?.RequiresPasswordSetup != true;

    /// <summary>Mở trang đầu tiên. <paramref name="page"/> chỉ dùng cho dev/QA (xem DevOptions).</summary>
    public void Start(string? page = null)
    {
        var parts = (page ?? "home").Split(':', 2);
        var arg = parts.Length > 1 ? parts[1] : null;
        switch (parts[0].ToLowerInvariant())
        {
            case "auth": _nav.NavigateTo<AuthViewModel>(AuthTab.SignIn); break;
            case "signup": _nav.NavigateTo<AuthViewModel>(AuthTab.SignUp); break;
            case "cinemas": _nav.NavigateTo<CinemasViewModel>(new CinemasArgs(arg)); break;
            case "categories": _nav.NavigateTo<CategoriesViewModel>(new CategoriesArgs()); break;
            case "movie": _nav.NavigateTo<MovieDetailViewModel>(new MovieDetailArgs(arg ?? "m01")); break;
            case "booking": { var p = (arg ?? "").Split("@"); var q = p.Length > 1 ? p[1].Split(":") : []; _nav.NavigateTo<BookingViewModel>(new BookingArgs(p[0], q.Length > 0 && int.TryParse(q[0], out var st) ? st : 1, q.Length > 1 ? q[1] : null)); break; }
            case "tickets": _nav.NavigateTo<MyTicketsViewModel>(); break;
            default: _nav.NavigateTo<HomeViewModel>(); break;
        }
    }
}
