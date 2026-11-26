using CineBook.Helpers;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CineBook.Services.Mock;
using CineBook.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace CineBook;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
        await _host.StartAsync();

        var dev = DevOptions.Parse(e.Args);
        if (dev.ShotPath is not null) System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Error;
        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
        _host.Services.GetRequiredService<MainViewModel>().Start(dev.Page);
        dev.ScheduleScreenshot(window);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // TODO-BACKEND: MockMovieService → ApiMovieService (HttpClient + JSON)
        services.AddSingleton<IMovieService, MockMovieService>();
        // TODO-BACKEND: MockCinemaService → ApiCinemaService
        services.AddSingleton<ICinemaService, MockCinemaService>();
        // TODO-BACKEND: MockShowtimeService → ApiShowtimeService
        services.AddSingleton<IShowtimeService, MockShowtimeService>();
        // TODO-BACKEND: MockSeatService → ApiSeatService (+ SignalR cho trạng thái ghế realtime)
        services.AddSingleton<ISeatService, MockSeatService>();
        // TODO-BACKEND: MockAuthService → ApiAuthService (JWT + refresh token, lưu bằng DPAPI)
        services.AddSingleton<IAuthService, MockAuthService>();
        // TODO-BACKEND: MockBookingService → ApiBookingService
        services.AddSingleton<IBookingService, MockBookingService>();
        // TODO-BACKEND: MockLocationService → WindowsLocationService (hoặc giữ mock nếu chỉ cần chọn thành phố)
        services.AddSingleton<ILocationService, MockLocationService>();

        // ---- Hạ tầng frontend (backend không cần đụng) ----
        services.AddSingleton<AppState>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<HeaderViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        // Mỗi trang là Transient để mỗi lần mở có state mới.
        services.AddTransient<HomeViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CinemasViewModel>();
        services.AddTransient<MovieDetailViewModel>();
        services.AddTransient<AuthViewModel>();
        services.AddTransient<MyTicketsViewModel>();
        services.AddTransient<BookingViewModel>();
    }
}