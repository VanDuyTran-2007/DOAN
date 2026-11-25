using System;

namespace CineBook.Api.Data;

public class Showtime
{
	public string Id { get; set; } = string.Empty;

	public string MovieId { get; set; } = string.Empty;

	public string AuditoriumId { get; set; } = string.Empty;

	public DateTime StartTimeUtc { get; set; }

	public DateTime EndTimeUtc { get; set; }

	public string Format { get; set; } = string.Empty;

	public string Language { get; set; } = string.Empty;

	public decimal BasePrice { get; set; }

	public Movie? Movie { get; set; }

	public Auditorium? Auditorium { get; set; }
}
