using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[ApiController]
[Route("api/auth/external")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ExternalAuthController(
    CineBookDbContext db, ExternalAuthFlows flows, IAuthenticationSchemeProvider schemes) : ControllerBase
{
    [HttpPost("{provider}/start")]
    public async Task<IActionResult> Start(string provider, ExternalStartDto request)
    {
        var scheme = provider.ToLowerInvariant() switch { "google" => "Google", "facebook" => "Facebook", _ => null };
        if (scheme is null) return NotFound();
        if (await schemes.GetSchemeAsync(scheme) is null)
            return Problem(statusCode: 503, detail: $"Máy chủ chưa cấu hình đăng nhập {scheme}.");
        if (!Request.IsHttps)
            return Problem(statusCode: 400, detail: "Đăng nhập qua mạng xã hội cần địa chỉ API HTTPS.");
        var owner = request.Link ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        if (request.Link && owner is null) return Unauthorized();

        var id = flows.Create(scheme, request.Port, request.Challenge, owner);
        return id is null
            ? Problem(statusCode: 503, detail: "Đang có nhiều yêu cầu đăng nhập. Hãy thử lại.")
            : Ok(new { Flow = id.Value, BrowserPath = $"api/auth/external/begin/{id.Value:N}" });
    }

    [HttpGet("begin/{id:guid}")]
    public IActionResult Begin(Guid id)
    {
        if (!Request.IsHttps) return BadRequest();
        var provider = flows.Begin(id);
        if (provider is null) return BadRequest("Yêu cầu đã hết hạn hoặc được mở. Hãy thử lại từ CineBook.");
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        properties.Items[ExternalAuthFlows.FlowKey] = id.ToString("N");
        return Challenge(properties, provider);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(ExternalCompleteDto request, CancellationToken ct)
    {
        if (!Request.IsHttps) return BadRequest();
        var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = flows.Redeem(request.Flow, request.Code, request.Verifier, owner);
        if (profile is null)
            return Problem(statusCode: 400, detail: "Mã xác thực đã hết hạn hoặc không hợp lệ. Hãy thử lại.");

        var link = await db.ExternalLogins.Include(row => row.User)
            .SingleOrDefaultAsync(row => row.Provider == profile.Provider && row.Subject == profile.Subject, ct);
        if (profile.Owner is { } linkedOwner)
            return await LinkAsync(profile, linkedOwner, link, ct);

        var user = link?.User;
        if (user is null)
        {
            var email = profile.Email?.Trim().ToLowerInvariant();
            if (email is not { Length: > 0 and <= 254 } || !new EmailAddressAttribute().IsValid(email))
                return Problem(statusCode: 422, detail: "Nhà cung cấp chưa trả về email hợp lệ. Hãy kiểm tra email và quyền chia sẻ email.");
            if (await db.Users.AnyAsync(row => row.Email == email, ct)) return ExistingEmailProblem();

            var name = profile.FullName?.Trim();
            if (string.IsNullOrWhiteSpace(name)) name = email.Split('@', 2)[0];
            user = new User
            {
                Id = "u-" + Guid.NewGuid().ToString("N"),
                FullName = name.Length <= 100 ? name : name[..100],
                Email = email,
                CreatedAtUtc = DateTime.UtcNow,
                SessionVersion = Guid.NewGuid()
            };
            db.ExternalLogins.Add(new ExternalLogin { Provider = profile.Provider, Subject = profile.Subject, User = user });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (AuthController.IsDuplicateContact(ex))
            {
                return Problem(statusCode: 409, detail: "Tài khoản vừa được tạo hoặc liên kết. Hãy thử đăng nhập lại.");
            }
        }
        if (user.IsLocked) return Problem(statusCode: 423, detail: "Tài khoản đã bị khóa.");

        await AuthController.StartSessionAsync(HttpContext, user);
        return Ok(new { Success = true, User = AuthController.ToUser(user) });
    }

    private async Task<IActionResult> LinkAsync(ExternalProfile profile, string owner, ExternalLogin? link, CancellationToken ct)
    {
        if (link is not null && link.UserId != owner)
            return Problem(statusCode: 409, detail: "Tài khoản mạng xã hội này đã liên kết với tài khoản CineBook khác.");

        var user = await db.Users.SingleOrDefaultAsync(row => row.Id == owner && !row.IsLocked, ct);
        if (user is null || user.PasswordHash.Length == 0) return Unauthorized();
        if (link is null)
        {
            db.ExternalLogins.Add(new ExternalLogin { Provider = profile.Provider, Subject = profile.Subject, UserId = owner });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (AuthController.IsDuplicateContact(ex))
            {
                return Problem(statusCode: 409, detail: $"Liên kết {profile.Provider} đã tồn tại.");
            }
        }
        return Ok(new { Success = true, User = AuthController.ToUser(user) });
    }

    private ObjectResult ExistingEmailProblem() => Problem(statusCode: 409,
        detail: "Email đã có tài khoản CineBook. Hãy đăng nhập bằng mật khẩu và liên kết trong Chi tiết tài khoản.");
}

public record ExternalStartDto(
    [Range(1024, 65535)] int Port,
    [Required, RegularExpression("^[A-Za-z0-9_-]{43}$")] string Challenge,
    bool Link = false);

public record ExternalCompleteDto(
    Guid Flow,
    [Required, RegularExpression("^[A-Za-z0-9_-]{43}$")] string Code,
    [Required, RegularExpression("^[A-Za-z0-9_-]{43}$")] string Verifier);
