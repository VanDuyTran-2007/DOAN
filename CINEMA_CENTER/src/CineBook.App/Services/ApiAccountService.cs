using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CineBook.Models;
using CineBook.Services.Interfaces;

namespace CineBook.Services;

public sealed class AccountApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class ApiAccountService(HttpClient client, AppState state, ILocationService locations) : IAuthService, IAccountService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public User? CurrentUser { get; private set; }
    public event EventHandler? AuthStateChanged;

    public Task<AuthResult> SignInAsync(string identity, string password, CancellationToken ct = default) =>
        AuthenticateAsync("api/auth/sign-in", new { identity, password }, ct);

    public Task<AuthResult> SignUpAsync(SignUpRequest request, CancellationToken ct = default) =>
        AuthenticateAsync("api/auth/sign-up", request, ct);

    private async Task<AuthResult> AuthenticateAsync<T>(string path, T request, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(path, request, ct);
        if (!response.IsSuccessStatusCode)
            return new AuthResult(false, await ReadErrorAsync(response, ct), null);

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
        using var response = await client.GetAsync("api/account", ct);
        await CheckAsync(response, ct);
        return await ReadAsync<AccountProfile>(response, ct);
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

    public async Task<byte[]> GetAvatarAsync(CancellationToken ct)
    {
        using var response = await client.GetAsync("api/account/avatar", ct);
        await CheckAsync(response, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<AccountProfile> UploadAvatarAsync(byte[] png, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var photo = new ByteArrayContent(png) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } };
        form.Add(photo, "photo", "avatar.png");
        using var response = await client.PutAsync("api/account/avatar", form, ct);
        await CheckAsync(response, ct);
        return await ReadAsync<AccountProfile>(response, ct);
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

    private void ApplyPreferredCity(AccountProfile profile)
    {
        state.PreferredCity = locations.Cities.FirstOrDefault(city =>
            string.Equals(city.City, profile.City, StringComparison.OrdinalIgnoreCase));
        if (state.PreferredCity is { } preferred) state.SelectedCity = preferred;
    }

    private void SetCurrentUser(User? user)
    {
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
