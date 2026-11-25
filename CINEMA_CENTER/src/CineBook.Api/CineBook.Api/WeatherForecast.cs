using System;

namespace CineBook.Api;

public class WeatherForecast
{
	public DateOnly Date { get; set; }

	public int TemperatureC { get; set; }

	public int TemperatureF => 32 + (int)((double)TemperatureC / 0.5556);

	public string? Summary { get; set; }
}
