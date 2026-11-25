namespace CineBook.Api.Data;

public class Cinema
{
	public string Id { get; set; } = string.Empty;

	public string BrandId { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Address { get; set; } = string.Empty;

	public string City { get; set; } = string.Empty;

	public string District { get; set; } = string.Empty;

	public double Latitude { get; set; }

	public double Longitude { get; set; }

	public Brand? Brand { get; set; }
}
