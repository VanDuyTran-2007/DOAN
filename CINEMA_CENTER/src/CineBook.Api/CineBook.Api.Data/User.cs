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
	public byte[]? AvatarBytes { get; set; }
	public string? AvatarContentType { get; set; }
	public int AvatarChanges { get; set; }
	public DateTime? AvatarCooldownUntilUtc { get; set; }
	public string? SecondaryEmail1 { get; set; }
	public string? SecondaryEmail2 { get; set; }
	public string City { get; set; } = string.Empty;
	public string District { get; set; } = string.Empty;
	public int PasswordCheckFailures { get; set; }
	public Guid SessionVersion { get; set; }
	public byte[] Version { get; set; } = [];

	public string MemberTier { get; set; } = "Standard";

	public DateOnly? BirthDate { get; set; }

	public bool IsLocked { get; set; }

	public DateTime CreatedAtUtc { get; set; }
}
