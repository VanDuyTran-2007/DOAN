using System;
using System.Linq;
using System.Threading.Tasks;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[ApiController]
[Route("api/showtimes")]
public class ShowtimesController : ControllerBase
{
	private readonly CineBookDbContext _db;

	private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

	public ShowtimesController(CineBookDbContext db)
	{
		_db = db;
	}

	[HttpGet]
	public async Task<IActionResult> GetShowtimes([FromQuery] string? movieId, [FromQuery] string? cinemaId, [FromQuery] DateTime? date)
	{
		IQueryable<Showtime> query = _db.Showtimes.Include((Showtime s) => s.Auditorium).AsQueryable();
		if (!string.IsNullOrWhiteSpace(movieId))
		{
			query = query.Where((Showtime s) => s.MovieId == movieId);
		}
		if (!string.IsNullOrWhiteSpace(cinemaId))
		{
			query = query.Where((Showtime s) => s.Auditorium != null && s.Auditorium.CinemaId == cinemaId);
		}
		if (date.HasValue)
		{
			DateTime startUtc = date.Value.Date.AddHours(-7.0);
			DateTime endUtc = startUtc.AddDays(1.0);
			query = query.Where((Showtime s) => s.StartTimeUtc >= startUtc && s.StartTimeUtc < endUtc);
		}
		var result = (await query.OrderBy((Showtime s) => s.StartTimeUtc).ToListAsync()).Select((Showtime s) => new
		{
			Id = s.Id,
			MovieId = s.MovieId,
			CinemaId = (s.Auditorium?.CinemaId ?? ""),
			AuditoriumId = s.AuditoriumId,
			AuditoriumName = (s.Auditorium?.Name ?? ""),
			StartTime = new DateTimeOffset(DateTime.SpecifyKind(s.StartTimeUtc, DateTimeKind.Utc)).ToOffset(VnOffset),
			Format = s.Format,
			Language = s.Language,
			BasePrice = s.BasePrice
		});
		return Ok(result);
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetById(string id)
	{
		Showtime s = await _db.Showtimes.Include((Showtime x) => x.Auditorium).FirstOrDefaultAsync((Showtime x) => x.Id == id);
		if (s == null)
		{
			return NotFound();
		}
		return Ok(new
		{
			Id = s.Id,
			MovieId = s.MovieId,
			CinemaId = (s.Auditorium?.CinemaId ?? ""),
			AuditoriumId = s.AuditoriumId,
			AuditoriumName = (s.Auditorium?.Name ?? ""),
			StartTime = new DateTimeOffset(DateTime.SpecifyKind(s.StartTimeUtc, DateTimeKind.Utc)).ToOffset(VnOffset),
			Format = s.Format,
			Language = s.Language,
			BasePrice = s.BasePrice
		});
	}
}
