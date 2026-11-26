using System;

namespace CineBook.Api.Data;

public class Booking
{
	public string Id { get; set; } = string.Empty;

	public string Code { get; set; } = string.Empty;

	public string? UserId { get; set; }

	public string ShowtimeId { get; set; } = string.Empty;

	public string Status { get; set; } = "Pending";

	public decimal TotalAmount { get; set; }

	public string? IdempotencyKey { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime ExpiresAtUtc { get; set; }

	public Showtime? Showtime { get; set; }
}
