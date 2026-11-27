using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/account")]
public sealed class AccountController(CineBookDbContext db, AccountPasswords passwords) : ControllerBase
{
    private const int AvatarLimit = 3;
    private const int MaxAvatarBytes = 512 * 1024;
    private const int PasswordCheckLimit = 3;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await FindAccountAsync(ct);
        return user is null ? Unauthorized() : Ok(await ReadProfileAsync(user, ct));
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateAccountDto request, CancellationToken ct)
    {
        var user = await FindAccountAsync(ct);
        if (user is null) return Unauthorized();

        var name = request.FullName.Trim();
        var primary = NormalizeEmail(request.Email)!;
        var secondary1 = NormalizeEmail(request.SecondaryEmail1);
        var secondary2 = NormalizeEmail(request.SecondaryEmail2);
        if (name.Length < 2 || primary == secondary1 || primary == secondary2 ||
            secondary1 is not null && secondary1 == secondary2)
            return Problem(statusCode: 400, detail: "Họ tên phải có ít nhất 2 ký tự và các email phải khác nhau.");

        user.FullName = name;
        user.Email = primary;
        user.SecondaryEmail1 = secondary1;
        user.SecondaryEmail2 = secondary2;
        user.Phone = request.Phone.Trim();
        user.City = request.City?.Trim() ?? "";
        user.District = request.District?.Trim() ?? "";
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return RetryConflict(); }
        catch (DbUpdateException ex) when (AuthController.IsDuplicateContact(ex))
        {
            return Problem(statusCode: 409, detail: "Email hoặc số điện thoại đang được tài khoản khác sử dụng.");
        }

        return Ok(await ReadProfileAsync(user, ct));
    }

    [HttpGet("avatar")]
    public async Task<IActionResult> GetAvatar(CancellationToken ct)
    {
        var user = await FindAccountAsync(ct);
        if (user?.AvatarBytes is null || user.AvatarContentType is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(user.AvatarBytes, user.AvatarContentType);
    }

    [HttpPut("avatar")]
    [RequestSizeLimit(MaxAvatarBytes + 8192)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAvatarBytes)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile photo, CancellationToken ct)
    {
        var user = await FindAccountAsync(ct);
        if (user is null) return Unauthorized();

        var now = DateTime.UtcNow;
        if (user.AvatarCooldownUntilUtc > now)
            return Problem(statusCode: 429, detail: "Đã đổi ảnh 3 lần. Vui lòng chờ hết thời gian chờ.");
        if (photo.Length is <= 0 or > MaxAvatarBytes)
            return Problem(statusCode: 400, detail: "Ảnh phải nhỏ hơn 512 KB.");

        using var data = new MemoryStream();
        await photo.CopyToAsync(data, ct);
        var bytes = data.ToArray();
        if (!IsAvatarPng(bytes))
            return Problem(statusCode: 400, detail: "Ảnh đại diện gửi lên phải là PNG hợp lệ, tối đa 512 × 512 pixel.");

        if (user.AvatarCooldownUntilUtc is not null)
        {
            user.AvatarChanges = 0;
            user.AvatarCooldownUntilUtc = null;
        }
        user.AvatarBytes = bytes;
        user.AvatarContentType = "image/png";
        if (++user.AvatarChanges == AvatarLimit)
            user.AvatarCooldownUntilUtc = now.AddDays(1);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return RetryConflict(); }
        return Ok(await ReadProfileAsync(user, ct));
    }

    [HttpPost("password/verify")]
    public async Task<IActionResult> VerifyPassword(VerifyAccountPasswordDto request, CancellationToken ct)
    {
        var user = await FindAccountAsync(ct);
        if (user is null) return Unauthorized();
        if (user.PasswordCheckFailures >= PasswordCheckLimit) return PasswordCheckLocked();

        var valid = passwords.Verify(user, request.Password, out var upgrade);
        user.PasswordCheckFailures = valid ? 0 : user.PasswordCheckFailures + 1;
        if (valid && upgrade)
            user.PasswordHash = passwords.Hash(user, request.Password);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return RetryConflict(); }

        if (valid) return NoContent();
        return user.PasswordCheckFailures >= PasswordCheckLimit
            ? PasswordCheckLocked()
            : Problem(statusCode: 400,
                detail: $"Mật khẩu chưa đúng. Còn {PasswordCheckLimit - user.PasswordCheckFailures} lần thử.");
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangeAccountPasswordDto request, CancellationToken ct)
    {
        var user = await FindAccountAsync(ct);
        if (user is null) return Unauthorized();
        if (request.NewPassword != request.Confirmation || request.NewPassword == request.CurrentPassword)
            return Problem(statusCode: 400, detail: "Mật khẩu mới phải khác mật khẩu cũ và khớp ô xác nhận.");
        if (!passwords.Verify(user, request.CurrentPassword, out _))
            return Problem(statusCode: 400, detail: "Mật khẩu cũ chưa đúng.");

        user.PasswordHash = passwords.Hash(user, request.NewPassword);
        user.PasswordCheckFailures = 0;
        user.SessionVersion = Guid.NewGuid();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return RetryConflict(); }

        await AuthController.StartSessionAsync(HttpContext, user);
        return NoContent();
    }

    [HttpGet("vouchers")]
    public async Task<IActionResult> GetVouchers(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return Ok(await db.Vouchers.AsNoTracking().Where(voucher => voucher.StartsAtUtc <= now && voucher.EndsAtUtc > now)
            .OrderBy(voucher => voucher.EndsAtUtc)
            .Select(voucher => new { voucher.Code, voucher.Title, voucher.Description, voucher.EndsAtUtc })
            .ToListAsync(ct));
    }

    private Task<User?> FindAccountAsync(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.Users.SingleOrDefaultAsync(user => user.Id == id && !user.IsLocked, ct);
    }

    private async Task<object> ReadProfileAsync(User user, CancellationToken ct)
    {
        var totalSpent = await db.Bookings.Where(booking => booking.UserId == user.Id && booking.Status == "Paid")
            .SumAsync(booking => booking.TotalAmount, ct);
        var cooldown = user.AvatarCooldownUntilUtc > DateTime.UtcNow
            ? new DateTimeOffset(DateTime.SpecifyKind(user.AvatarCooldownUntilUtc.Value, DateTimeKind.Utc))
            : (DateTimeOffset?)null;
        return new
        {
            user.Id, user.FullName, user.Email, user.Phone, user.SecondaryEmail1, user.SecondaryEmail2,
            user.City, user.District, user.BirthDate, user.MemberTier, TotalSpent = totalSpent,
            HasAvatar = user.AvatarBytes is not null, AvatarCooldownUntil = cooldown,
            AvatarChangesRemaining = cooldown is not null ? 0 : user.AvatarCooldownUntilUtc is null ? AvatarLimit - user.AvatarChanges : AvatarLimit,
            PasswordCheckLocked = user.PasswordCheckFailures >= PasswordCheckLimit
        };
    }

    private ObjectResult RetryConflict() => Problem(statusCode: 409, detail: "Tài khoản vừa thay đổi. Vui lòng thử lại.");
    private ObjectResult PasswordCheckLocked() => Problem(statusCode: 423,
        detail: "Đã nhập sai 3 lần. Hãy đổi mật khẩu để mở lại tính năng xác thực.");
    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static bool IsAvatarPng(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 45 || !bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
            BinaryPrimitives.ReadUInt32BigEndian(bytes[8..]) != 13 || !bytes[12..16].SequenceEqual("IHDR"u8))
            return false;

        return BinaryPrimitives.ReadUInt32BigEndian(bytes[16..]) is > 0 and <= 512 &&
            BinaryPrimitives.ReadUInt32BigEndian(bytes[20..]) is > 0 and <= 512 &&
            bytes[^12..^4].SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68 });
    }
}

public record UpdateAccountDto(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, StringLength(254), EmailAddress] string Email,
    [Required, RegularExpression("^0[0-9]{9}$")] string Phone,
    [StringLength(254), EmailAddress] string? SecondaryEmail1,
    [StringLength(254), EmailAddress] string? SecondaryEmail2,
    [StringLength(100)] string? City,
    [StringLength(100)] string? District);

public record VerifyAccountPasswordDto([Required, StringLength(256)] string Password);

public record ChangeAccountPasswordDto(
    [Required, StringLength(256)] string CurrentPassword,
    [Required, StringLength(256, MinimumLength = 8)] string NewPassword,
    [Required, StringLength(256)] string Confirmation);
