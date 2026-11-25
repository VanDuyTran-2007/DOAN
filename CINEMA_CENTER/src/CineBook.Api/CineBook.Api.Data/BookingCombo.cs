namespace CineBook.Api.Data;

public class BookingCombo
{
	public string BookingId { get; set; } = string.Empty;

	public string ComboId { get; set; } = string.Empty;

	public int Qty { get; set; }

	public decimal UnitPrice { get; set; }
}
