using System;

namespace CineBook.Api.Data;

public class User
{
	public string Id { get; set; } = string.Empty;

	public string FullName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string Phone { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	public string AvatarUrl { get; set; } = string.Empty;

	public string MemberTier { get; set; } = "Standard";

	public DateOnly? BirthDate { get; set; }

	public bool IsLocked { get; set; }

	public DateTime CreatedAtUtc { get; set; }
}
