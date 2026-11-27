using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

public sealed partial class AccountViewModel(IAccountService accounts, IAuthService auth, ISocialAuthService social,
    ILocationService locations, IToastService toast, INavigationService navigation, AppState state)
    : ViewModelBase
{
    private CancellationTokenSource? _page;

    [ObservableProperty] private AccountProfile? _profile;
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _secondaryEmail1 = "";
    [ObservableProperty] private string _secondaryEmail2 = "";
    [ObservableProperty] private string _city = "";
    [ObservableProperty] private string _district = "";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _mustSetPassword;
    [ObservableProperty] private int _selectedTab;

    public IReadOnlyList<string> Cities { get; } = locations.Cities.Select(location => location.City).ToArray();
    public SectionState<AccountVoucher> Vouchers { get; } = new(accounts.GetVouchersAsync);
    public AppState State { get; } = state;
    public bool HasProfile => Profile is not null;
    public bool CanChangeAvatar => Profile is { } profile &&
        (profile.AvatarCooldownUntil is null || profile.AvatarCooldownUntil <= DateTimeOffset.UtcNow);
    public bool CanVerify => Profile is { PasswordCheckLocked: false };

    partial void OnProfileChanged(AccountProfile? value)
    {
        OnPropertyChanged(nameof(HasProfile));
        OnPropertyChanged(nameof(CanChangeAvatar));
        OnPropertyChanged(nameof(CanVerify));
        OnPropertyChanged(nameof(GoogleLinkLabel));
        OnPropertyChanged(nameof(FacebookLinkLabel));
        LinkProviderCommand.NotifyCanExecuteChanged();
    }

    public string GoogleLinkLabel => ProviderLinked("Google") ? "Google đã liên kết" : "Liên kết Google";
    public string FacebookLinkLabel => ProviderLinked("Facebook") ? "Facebook đã liên kết" : "Liên kết Facebook";

    private bool ProviderLinked(string provider) => Profile?.LinkedProviders?.Contains(provider) == true;
    private bool CanLinkProvider(string provider) => Profile is not null && !MustSetPassword && !ProviderLinked(provider);

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        _page = new CancellationTokenSource();
        MustSetPassword = auth.CurrentUser?.RequiresPasswordSetup == true;
        if (MustSetPassword) SelectedTab = 1;
        await ReloadAsync();
    }

    public override void OnNavigatedFrom()
    {
        _page?.Cancel();
        _page?.Dispose();
        _page = null;
        Vouchers.Cancel();
    }

    [RelayCommand]
    private Task ReloadAsync() => RunAsync(async ct =>
    {
        var profile = await accounts.GetProfileAsync(ct);
        FillEditor(profile);
        if (MustSetPassword) return;
        await Vouchers.LoadAsync();
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        var changes = new AccountChanges(FullName.Trim(), Email.Trim(), Phone.Trim(),
            OptionalEmail(SecondaryEmail1), OptionalEmail(SecondaryEmail2), City.Trim(), District.Trim());
        FillEditor(await accounts.SaveAsync(changes, ct));
        toast.Show("Đã lưu thông tin tài khoản.", ToastKind.Success);
    });

    public Task UploadAsync(string filePath) => RunAsync(async ct =>
    {
        var png = await Task.Run(() => PrepareAvatar(filePath), ct);
        Profile = await accounts.UploadAvatarAsync(png, ct);
        toast.Show("Đã đổi ảnh đại diện.", ToastKind.Success);
    });

    public async Task VerifyAsync(string password, CancellationToken ct)
    {
        try { await accounts.VerifyPasswordAsync(password, ct); }
        catch (AccountApiException ex) when ((int)ex.StatusCode == 423)
        {
            if (Profile is { } profile) Profile = profile with { PasswordCheckLocked = true };
            throw;
        }
    }

    public async Task ChangePasswordAsync(string current, string next, string confirmation, CancellationToken ct)
    {
        await accounts.ChangePasswordAsync(current, next, confirmation, ct);
        if (Profile is { } profile) Profile = profile with { PasswordCheckLocked = false };
        toast.Show("Đã đổi mật khẩu.", ToastKind.Success);
    }

    public async Task<bool> SetInitialPasswordAsync(string password, string confirmation)
    {
        if (!MustSetPassword || IsBusy) return false;
        if (password.Length < 8 || password != confirmation)
        {
            Error = "Mật khẩu cần ít nhất 8 ký tự và phải khớp ô xác nhận.";
            return false;
        }

        var saved = false;
        await RunAsync(async ct =>
        {
            await accounts.SetInitialPasswordAsync(password, confirmation, ct);
            MustSetPassword = false;
            if (Profile is { } profile) Profile = profile with { RequiresPasswordSetup = false };
            saved = true;
            toast.Show("Đã đặt mật khẩu cho tài khoản CineBook.", ToastKind.Success);
            await Vouchers.LoadAsync();
        });
        return saved;
    }

    [RelayCommand(CanExecute = nameof(CanLinkProvider))]
    private Task LinkProviderAsync(string provider) => RunAsync(async ct =>
    {
        var result = await social.AuthenticateAsync(provider, link: true, ct);
        if (!result.Success) { Error = result.Error; return; }
        Profile = await accounts.GetProfileAsync(ct);
        toast.Show($"Đã liên kết {provider}.", ToastKind.Success);
    });

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await auth.SignOutAsync();
        navigation.NavigateTo<HomeViewModel>();
    }

    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy || _page is null) return;
        var ct = _page.Token;
        Error = null;
        IsBusy = true;
        try { await action(ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { Error = ex is AccountApiException or InvalidDataException ? ex.Message : "Không thể hoàn thành yêu cầu. Hãy kiểm tra kết nối với API hoặc thử lại."; }
        finally { IsBusy = false; }
    }

    private void FillEditor(AccountProfile profile)
    {
        MustSetPassword = profile.RequiresPasswordSetup;
        if (MustSetPassword) SelectedTab = 1;
        Profile = profile;
        FullName = profile.FullName;
        Email = profile.Email;
        Phone = profile.Phone;
        SecondaryEmail1 = profile.SecondaryEmail1 ?? "";
        SecondaryEmail2 = profile.SecondaryEmail2 ?? "";
        City = profile.City;
        District = profile.District;
    }

    private static string? OptionalEmail(string email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim();

    private static byte[] PrepareAvatar(string path)
    {
        using var input = File.OpenRead(path);
        if (input.Length > 12 * 1024 * 1024) throw new InvalidDataException("Hãy chọn ảnh dưới 12 MB.");
        try
        {
            var frame = BitmapDecoder.Create(input, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
            var scale = Math.Min(1, 300d / Math.Max(frame.PixelWidth, frame.PixelHeight));
            BitmapSource source = scale < 1 ? new TransformedBitmap(frame, new ScaleTransform(scale, scale)) : frame;
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0)));
            using var output = new MemoryStream();
            encoder.Save(output);
            return output.ToArray();
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException)
        {
            throw new InvalidDataException("Ảnh không hợp lệ. Hãy chọn tệp PNG hoặc JPEG.", ex);
        }
    }
}
