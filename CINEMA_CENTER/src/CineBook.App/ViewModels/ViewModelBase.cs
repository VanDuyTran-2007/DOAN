using CommunityToolkit.Mvvm.ComponentModel;

namespace CineBook.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>Đang gọi service (không chặn UI thread).</summary>
    [ObservableProperty] private bool _isBusy;

    /// <summary>Gọi khi trang được mở. Mọi thao tác nạp dữ liệu phải async.</summary>
    public virtual Task OnNavigatedToAsync(object? parameter) => Task.CompletedTask;

    /// <summary>Gọi khi rời trang (huỷ timer, huỷ request...).</summary>
    public virtual void OnNavigatedFrom() { }
}

/// <summary>Trạng thái nạp của một khu vực (section) trong trang: Loading / Empty / Error / Content.</summary>
public enum LoadState { Loading, Content, Empty, Error }

/// <summary>
/// Gói gọn logic Loading/Empty/Error cho từng section. View chỉ bind <see cref="State"/>.
/// </summary>
public partial class SectionState<T> : ObservableObject
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<T>>> _loader;
    private CancellationTokenSource? _cts;

    public SectionState(Func<CancellationToken, Task<IReadOnlyList<T>>> loader) => _loader = loader;

    public System.Collections.ObjectModel.ObservableCollection<T> Items { get; } = [];

    [ObservableProperty] private LoadState _state = LoadState.Loading;
    [ObservableProperty] private string? _error;

    public bool IsLoading => State == LoadState.Loading;
    public bool IsEmpty => State == LoadState.Empty;
    public bool IsError => State == LoadState.Error;
    public bool HasContent => State == LoadState.Content;

    partial void OnStateChanged(LoadState value)
    {
        OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsError)); OnPropertyChanged(nameof(HasContent));
    }

    public async Task LoadAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        State = LoadState.Loading;
        try
        {
            var data = await _loader(_cts.Token);
            Items.Clear();
            foreach (var i in data) Items.Add(i);
            State = Items.Count == 0 ? LoadState.Empty : LoadState.Content;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Error = ex.Message;
            State = LoadState.Error;
        }
    }

    public void Cancel() => _cts?.Cancel();
}
