using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>Trang "Vé của tôi".</summary>
public partial class MyTicketsViewModel : ViewModelBase
{
    private readonly IBookingService _bookings;
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;

    public MyTicketsViewModel(IBookingService bookings, IAuthService auth, INavigationService nav)
    {
        _bookings = bookings; _auth = auth; _nav = nav;
        Tickets = new SectionState<Booking>(ct => LoadAsync(ct));
    }

    public SectionState<Booking> Tickets { get; }
    public bool IsSignedIn => _auth.CurrentUser is not null;

    private Task<IReadOnlyList<Booking>> LoadAsync(CancellationToken ct)
        // TODO-BACKEND: GET /api/me/bookings?status=Paid → List<Booking> (cần access token). Lỗi 401 → yêu cầu đăng nhập lại.
        => _bookings.GetMyTicketsAsync(ct);

    public override Task OnNavigatedToAsync(object? parameter) => IsSignedIn ? Tickets.LoadAsync() : Task.CompletedTask;

    [RelayCommand] private void SignIn() => _nav.NavigateTo<AuthViewModel>(AuthTab.SignIn);
    [RelayCommand] private Task Retry() => Tickets.LoadAsync();
    [RelayCommand] private void GoHome() => _nav.NavigateTo<HomeViewModel>();
}
