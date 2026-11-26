using System;

namespace CineBook.Api.Data;

public class Movie
{
	public string Id { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string OriginalTitle { get; set; } = string.Empty;

	public string PosterUrl { get; set; } = string.Empty;

	public string BackdropUrl { get; set; } = string.Empty;

	public string TrailerUrl { get; set; } = string.Empty;

	public int DurationMinutes { get; set; }

	public string AgeRating { get; set; } = string.Empty;

	public DateOnly ReleaseDate { get; set; }

	public double Rating { get; set; }

	public string Status { get; set; } = string.Empty;

	public bool IsHot { get; set; }

	public string Description { get; set; } = string.Empty;

	public string Director { get; set; } = string.Empty;
}
