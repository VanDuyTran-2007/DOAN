using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
	private readonly CineBookDbContext _db;

	public AuthController(CineBookDbContext db)
	{
		_db = db;
	}

	private static string HashPassword(string password)
	{
		byte[] inArray = SHA256.HashData(Encoding.UTF8.GetBytes("CineBookSalt2026:" + password));
		return Convert.ToHexString(inArray);
	}

	private static bool VerifyPassword(string password, string storedHash)
	{
		if (storedHash.StartsWith("PLAIN:", StringComparison.Ordinal))
		{
			return storedHash.Substring("PLAIN:".Length) == password;
		}
		return string.Equals(HashPassword(password), storedHash, StringComparison.OrdinalIgnoreCase);
	}

	[HttpPost("sign-in")]
	public async Task<IActionResult> SignIn([FromBody] SignInDto req)
	{
		string id = req.Identity?.Trim() ?? "";
		User user = await _db.Users.FirstOrDefaultAsync((User u) => u.Email == id || u.Phone == id);
		if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
		{
			return Ok(new
			{
				Success = false,
				Error = "Email/SĐT hoặc mật khẩu không đúng",
				User = (object)null
			});
		}
		if (user.IsLocked)
		{
			return Ok(new
			{
				Success = false,
				Error = "Tài khoản đã bị khóa",
				User = (object)null
			});
		}
		return Ok(new
		{
			Success = true,
			Error = (string)null,
			User = new { user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl, user.MemberTier }
		});
	}

	[HttpPost("sign-up")]
	public async Task<IActionResult> SignUp([FromBody] SignUpDto req)
	{
		string email = req.Email.Trim();
		string phone = req.Phone.Trim();
		if (await _db.Users.AnyAsync((User u) => u.Email == email || u.Phone == phone))
		{
			return Ok(new
			{
				Success = false,
				Error = "Email hoặc số điện thoại đã được đăng ký",
				User = (object)null
			});
		}
		User user = new User
		{
			Id = "u-" + Guid.NewGuid().ToString("N").Substring(0, 10),
			FullName = req.FullName.Trim(),
			Email = email,
			Phone = phone,
			PasswordHash = HashPassword(req.Password),
			AvatarUrl = "",
			MemberTier = "Standard",
			BirthDate = (req.BirthDate.HasValue ? new DateOnly?(DateOnly.FromDateTime(req.BirthDate.Value)) : ((DateOnly?)null)),
			IsLocked = false,
			CreatedAtUtc = DateTime.UtcNow
		};
		_db.Users.Add(user);
		await _db.SaveChangesAsync();
		return Ok(new
		{
			Success = true,
			Error = (string)null,
			User = new { user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl, user.MemberTier }
		});
	}
}
