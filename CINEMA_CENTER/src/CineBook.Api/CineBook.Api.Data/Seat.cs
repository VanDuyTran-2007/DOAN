namespace CineBook.Api.Data;

public class Seat
{
	public string Id { get; set; } = string.Empty;

	public string AuditoriumId { get; set; } = string.Empty;

	public string Row { get; set; } = string.Empty;

	public int Number { get; set; }

	public string Type { get; set; } = string.Empty;

	public int GridColumn { get; set; }

	public int ColumnSpan { get; set; }

	public bool IsAvailable { get; set; }
}
