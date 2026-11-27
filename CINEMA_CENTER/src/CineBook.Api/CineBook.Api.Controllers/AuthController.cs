using System.Security.Claims;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(CineBookDbContext db, AccountPasswords passwords) : ControllerBase
{
    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(SignInDto request, CancellationToken ct)
    {
        var identity = request.Identity.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(row => row.Email == identity || row.Phone == identity, ct);
        if (user is null || !passwords.Verify(user, request.Password, out var upgrade))
            return Problem(statusCode: 401, detail: "Email/SĐT hoặc mật khẩu không đúng.");
        if (user.IsLocked)
            return Problem(statusCode: 423, detail: "Tài khoản đã bị khóa.");

        if (upgrade)
        {
            user.PasswordHash = passwords.Hash(user, request.Password);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Problem(statusCode: 409, detail: "Vui lòng đăng nhập lại."); }
        }

        await StartSessionAsync(HttpContext, user);
        return Ok(new { Success = true, User = ToUser(user) });
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp(SignUpDto request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var birthDate = request.BirthDate is { } value ? DateOnly.FromDateTime(value) : (DateOnly?)null;
        if (request.FullName.Trim().Length < 2 || birthDate is null || birthDate > today || birthDate < today.AddYears(-120))
            return Problem(statusCode: 400, detail: "Họ tên hoặc ngày sinh không hợp lệ.");

        var user = new User
        {
            Id = "u-" + Guid.NewGuid().ToString("N"),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            BirthDate = birthDate,
            CreatedAtUtc = DateTime.UtcNow,
            SessionVersion = Guid.NewGuid()
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        db.Users.Add(user);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsDuplicateContact(ex))
        {
            return Problem(statusCode: 409, detail: "Email hoặc số điện thoại đã được đăng ký.");
        }

        await StartSessionAsync(HttpContext, user);
        return Ok(new { Success = true, User = ToUser(user) });
    }

    [Authorize]
    [AllowDuringPasswordSetup]
    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOutAccount()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    internal static bool IsDuplicateContact(DbUpdateException error) =>
        error.InnerException is SqlException { Number: 2601 or 2627 };

    internal static object ToUser(User user) =>
        new { user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl, user.MemberTier,
            RequiresPasswordSetup = user.PasswordHash.Length == 0 };

    internal static Task StartSessionAsync(HttpContext context, User user)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new("session_version", user.SessionVersion.ToString("N")));
        if (user.PasswordHash.Length == 0) identity.AddClaim(new("password_setup", "true"));
        return context.SignInAsync(new ClaimsPrincipal(identity));
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class AllowDuringPasswordSetupAttribute : Attribute;
