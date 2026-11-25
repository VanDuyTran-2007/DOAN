using System;
using System.ComponentModel.DataAnnotations;

namespace CineBook.Api.Data;

public class SeatHold
{
	[Key]
	public string HoldId { get; set; } = string.Empty;

	public string ShowtimeId { get; set; } = string.Empty;

	public string? UserId { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime ExpiresAtUtc { get; set; }
}
