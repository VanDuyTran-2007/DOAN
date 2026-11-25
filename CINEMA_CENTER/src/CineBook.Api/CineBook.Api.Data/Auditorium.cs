namespace CineBook.Api.Data;

public class Auditorium
{
	public string Id { get; set; } = string.Empty;

	public string CinemaId { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public int TotalColumns { get; set; }

	public Cinema? Cinema { get; set; }
}
