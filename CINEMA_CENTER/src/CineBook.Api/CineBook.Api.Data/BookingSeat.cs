namespace CineBook.Api.Data;

public class BookingSeat
{
	public string BookingId { get; set; } = string.Empty;

	public string ShowtimeId { get; set; } = string.Empty;

	public string SeatId { get; set; } = string.Empty;

	public decimal Price { get; set; }

	public bool IsActive { get; set; }
}
