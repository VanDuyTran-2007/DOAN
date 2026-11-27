using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using CineBook.Models;
using CineBook.Services.Interfaces;

namespace CineBook.Services;

public sealed class AccountApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class ApiAccountService(HttpClient client, AppState state, ILocationService locations)
    : IAuthService, IAccountService, ISocialAuthService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public User? CurrentUser { get; private set; }
    public event EventHandler? AuthStateChanged;
    private long _avatarRevision;

    public Task<AuthResult> SignInAsync(string identity, string password, CancellationToken ct = default) =>
        AuthenticateAsync("api/auth/sign-in", new { identity, password }, ct);

    public Task<AuthResult> SignUpAsync(SignUpRequest request, CancellationToken ct = default) =>
        AuthenticateAsync("api/auth/sign-up", request, ct);

    private async Task<AuthResult> AuthenticateAsync<T>(string path, T request, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(path, request, ct);
        if (!response.IsSuccessStatusCode)
            return new AuthResult(false, await ReadErrorAsync(response, ct), null,
                InvalidCredentials: path == "api/auth/sign-in" && response.StatusCode == HttpStatusCode.Unauthorized);

        var result = await ReadAsync<AuthResult>(response, ct);
        SetCurrentUser(result.User);
        if (result.Success && result.User is not null)
        {
            // Lỗi tải tùy chọn địa phương không làm mất phiên đăng nhập thành công.
            try { ApplyPreferredCity(await GetProfileAsync(ct)); }
            catch (Exception ex) when (ex is HttpRequestException or AccountApiException or TaskCanceledException) { }
        }
        return result;
    }

    public async Task<AuthResult> AuthenticateAsync(string provider, bool link, CancellationToken ct)
    {
        if (provider is not ("Google" or "Facebook"))
            throw new ArgumentOutOfRangeException(nameof(provider));
        if (client.BaseAddress is not { Scheme: "https" } origin)
            return new AuthResult(false, "Đăng nhập Google/Facebook cần địa chỉ API HTTPS.", null);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var token = timeout.Token;
        try
        {
            using var callback = new OAuthBrowserCallback();
            var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
            var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            using var start = await client.PostAsJsonAsync($"api/auth/external/{provider.ToLowerInvariant()}/start",
                new { port = callback.Port, challenge, link }, token);
            if (!start.IsSuccessStatusCode) return new AuthResult(false, await ReadErrorAsync(start, token), null);

            var launch = await ReadAsync<ExternalLaunch>(start, token);
            var browser = new Uri(origin, launch.BrowserPath);
            if (browser.GetLeftPart(UriPartial.Authority) != origin.GetLeftPart(UriPartial.Authority))
                throw new HttpRequestException("Địa chỉ đăng nhập không hợp lệ.");
            using var process = Process.Start(new ProcessStartInfo(browser.AbsoluteUri) { UseShellExecute = true });
            var code = await callback.ReceiveAsync(launch.Flow, token);
            if (string.IsNullOrEmpty(code)) return new AuthResult(false, "Đăng nhập mạng xã hội đã bị hủy hoặc từ chối quyền.", null);

            using var response = await client.PostAsJsonAsync("api/auth/external/complete",
                new { flow = launch.Flow, code, verifier }, token);
            if (!response.IsSuccessStatusCode) return new AuthResult(false, await ReadErrorAsync(response, token), null);

            var result = await ReadAsync<AuthResult>(response, token);
            if (result.Success && !link)
            {
                SetCurrentUser(result.User);
                try { ApplyPreferredCity(await GetProfileAsync(token)); }
                catch (Exception ex) when (ex is HttpRequestException or AccountApiException or OperationCanceledException) { }
            }
            return result;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            return new AuthResult(false, "Phiên đăng nhập đã hết hạn. Hãy thử lại.", null);
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record ExternalLaunch(Guid Flow, string BrowserPath);

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await client.PostAsync("api/auth/sign-out", null, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { }
        finally
        {
            if (client is SessionHttpClient session) session.ClearCookie();
            state.PreferredCity = null;
            SetCurrentUser(null);
        }
    }

    public async Task<AccountProfile> GetProfileAsync(CancellationToken ct)
    {
        var avatarRevision = _avatarRevision;
        using var response = await client.GetAsync("api/account", ct);
        await CheckAsync(response, ct);
        var profile = await ReadAsync<AccountProfile>(response, ct);
        await RefreshAvatarAsync(profile, avatarRevision, ct);
        return profile;
    }

    public async Task<AccountProfile> SaveAsync(AccountChanges changes, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync("api/account", changes, ct);
        await CheckAsync(response, ct);
        var profile = await ReadAsync<AccountProfile>(response, ct);
        if (CurrentUser is { } user)
        {
            SetCurrentUser(new User
            {
                Id = user.Id, FullName = profile.FullName, Email = profile.Email,
                Phone = profile.Phone, AvatarUrl = user.AvatarUrl, MemberTier = user.MemberTier
            });
        }
        ApplyPreferredCity(profile);
        return profile;
    }

    private async Task RefreshAvatarAsync(AccountProfile profile, long revision, CancellationToken ct)
    {
        if (!IsCurrentAvatarRequest(profile.Id, revision) || profile.RequiresPasswordSetup) return;
        if (!profile.HasAvatar)
        {
            state.AccountAvatar = null;
            return;
        }

        try
        {
            using var response = await client.GetAsync("api/account/avatar", ct);
            if (!response.IsSuccessStatusCode) return;
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (IsCurrentAvatarRequest(profile.Id, revision))
                state.AccountAvatar = DecodeAvatar(bytes);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or FormatException or NotSupportedException ||
                                   ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            // Lỗi ảnh tạm thời vẫn cho phép hiển thị thông tin hồ sơ.
        }
    }

    public async Task<AccountProfile> UploadAvatarAsync(byte[] png, CancellationToken ct)
    {
        var avatarRevision = _avatarRevision;
        var avatar = DecodeAvatar(png);
        using var form = new MultipartFormDataContent();
        var photo = new ByteArrayContent(png) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } };
        form.Add(photo, "photo", "avatar.png");
        using var response = await client.PutAsync("api/account/avatar", form, ct);
        await CheckAsync(response, ct);
        var profile = await ReadAsync<AccountProfile>(response, ct);
        if (IsCurrentAvatarRequest(profile.Id, avatarRevision))
        {
            _avatarRevision++;
            state.AccountAvatar = avatar;
        }
        return profile;
    }

    private bool IsCurrentAvatarRequest(string userId, long revision) =>
        _avatarRevision == revision && CurrentUser?.Id == userId;

    private static BitmapImage DecodeAvatar(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 300;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public async Task<IReadOnlyList<AccountVoucher>> GetVouchersAsync(CancellationToken ct)
    {
        using var response = await client.GetAsync("api/account/vouchers", ct);
        await CheckAsync(response, ct);
        return await ReadAsync<AccountVoucher[]>(response, ct);
    }

    public async Task VerifyPasswordAsync(string password, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("api/account/password/verify", new { password }, ct);
        await CheckAsync(response, ct);
    }

    public async Task ChangePasswordAsync(string current, string next, string confirmation, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync("api/account/password",
            new { currentPassword = current, newPassword = next, confirmation }, ct);
        await CheckAsync(response, ct);
    }

    public async Task SetInitialPasswordAsync(string next, string confirmation, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync("api/account/password/setup",
            new { newPassword = next, confirmation }, ct);
        await CheckAsync(response, ct);
        SetCurrentUser(await ReadAsync<User>(response, ct));
    }

    private void ApplyPreferredCity(AccountProfile profile)
    {
        state.PreferredCity = locations.Cities.FirstOrDefault(city =>
            string.Equals(city.City, profile.City, StringComparison.OrdinalIgnoreCase));
        if (state.PreferredCity is { } preferred) state.SelectedCity = preferred;
    }

    private void SetCurrentUser(User? user)
    {
        if (CurrentUser?.Id != user?.Id)
        {
            _avatarRevision++;
            state.AccountAvatar = null;
        }
        CurrentUser = user;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task CheckAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized) SetCurrentUser(null);
        throw new AccountApiException(response.StatusCode, await ReadErrorAsync(response, ct));
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync<T>(Json, ct)
        ?? throw new HttpRequestException("API trả về dữ liệu trống.");

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (problem.ValueKind == JsonValueKind.Object &&
                problem.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString()!;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException) { }
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Bạn cần đăng nhập lại.",
            HttpStatusCode.BadRequest => "Vui lòng kiểm tra thông tin đã nhập.",
            _ => "Không thể hoàn thành yêu cầu. Vui lòng thử lại."
        };
    }
}

internal sealed class SessionHttpClient(HttpClientHandler handler) : HttpClient(handler)
{
    public void ClearCookie()
    {
        foreach (Cookie cookie in handler.CookieContainer.GetAllCookies())
            cookie.Expired = true;
    }
}
