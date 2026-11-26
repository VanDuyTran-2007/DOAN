using CineBook.Models;
using CineBook.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace CineBook.Navigation;

// ---- Tham số truyền khi điều hướng ----
public enum AuthTab { SignIn, SignUp }
public record CategoriesArgs(string? Genre = null, MovieStatus? Status = null);
public record MovieDetailArgs(string MovieId)
{
    public bool OpenSchedule { get; init; }
    public bool PlayTrailerOnOpen { get; init; }

    // Khi quay lại qua lịch sử điều hướng, chỉ hiển thị ảnh bìa trailer.
    internal MovieDetailArgs ForHistory() => PlayTrailerOnOpen ? this with { PlayTrailerOnOpen = false } : this;
}
public record CinemasArgs(string? CinemaId = null);
/// <param name="Step">Chỉ dùng cho dev/QA: mở thẳng bước 2/3/4.</param>
/// <param name="DevSeats">Chỉ dùng cho dev/QA: tự chọn sẵn các ghế, ví dụ "F5,F6".</param>
public record BookingArgs(string ShowtimeId, int Step = 1, string? DevSeats = null);

/// <summary>
/// Điều hướng giữa các ViewModel. MainWindow bind ContentControl vào <see cref="Current"/>,
/// DataTemplate trong App.xaml tự map ViewModel → View.
/// </summary>
public interface INavigationService
{
    ViewModelBase? Current { get; }
    bool CanGoBack { get; }
    event EventHandler? Navigated;
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    void GoBack();
}

public class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _provider;
    private readonly Stack<(Type Type, object? Param)> _history = new();
    private (Type Type, object? Param)? _currentEntry;
    private ViewModelBase? _current;

    public NavigationService(IServiceProvider provider) => _provider = provider;

    public ViewModelBase? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public bool CanGoBack => _history.Count > 0;
    public event EventHandler? Navigated;

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
        => Go(typeof(TViewModel), parameter, pushHistory: true);

    public void GoBack()
    {
        if (_history.Count == 0) return;
        var prev = _history.Pop();
        Go(prev.Type, prev.Param, pushHistory: false);
    }

    private void Go(Type type, object? parameter, bool pushHistory)
    {
        if (pushHistory && _currentEntry is { } cur) _history.Push(cur);
        _current?.OnNavigatedFrom();

        // ViewModel đăng ký Transient → mỗi lần điều hướng là một instance mới.
        var vm = (ViewModelBase)_provider.GetRequiredService(type);
        _currentEntry = (type, parameter is MovieDetailArgs movieDetail ? movieDetail.ForHistory() : parameter);
        Current = vm;
        OnPropertyChanged(nameof(CanGoBack));
        Navigated?.Invoke(this, EventArgs.Empty);
        // Chạy nạp dữ liệu không chặn UI; ViewModel tự xử lý lỗi và IsBusy.
        _ = vm.OnNavigatedToAsync(parameter);
    }
}
