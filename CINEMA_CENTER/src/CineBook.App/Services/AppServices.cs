using System.Collections.ObjectModel;
using System.Windows.Media;
using CineBook.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CineBook.Services;

/// <summary>Trạng thái dùng chung toàn app (thành phố đang chọn...).</summary>
public partial class AppState : ObservableObject
{
    [ObservableProperty] private GeoLocation? _selectedCity;
    [ObservableProperty] private ImageSource? _accountAvatar;
    public GeoLocation? PreferredCity { get; set; }
}

public enum ToastKind { Info, Success, Error }

public class ToastItem
{
    public required string Message { get; init; }
    public ToastKind Kind { get; init; }
}

public interface IToastService
{
    ObservableCollection<ToastItem> Items { get; }
    void Show(string message, ToastKind kind = ToastKind.Info);
}

/// <summary>Snackbar: tự ẩn sau 3.5 giây.</summary>
public class ToastService : IToastService
{
    public ObservableCollection<ToastItem> Items { get; } = [];

    public async void Show(string message, ToastKind kind = ToastKind.Info)
    {
        var item = new ToastItem { Message = message, Kind = kind };
        Items.Add(item);
        await Task.Delay(3500);
        Items.Remove(item);
    }
}
