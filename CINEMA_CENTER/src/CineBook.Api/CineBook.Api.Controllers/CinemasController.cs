using System.Linq;
using System.Threading.Tasks;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[ApiController]
[Route("api")]
public class CinemasController : ControllerBase
{
	private readonly CineBookDbContext _db;

	public CinemasController(CineBookDbContext db)
	{
		_db = db;
	}

	[HttpGet("brands")]
	public async Task<IActionResult> GetBrands()
	{
		return Ok(await _db.Brands.ToListAsync());
	}

	[HttpGet("cinemas")]
	public async Task<IActionResult> GetCinemas()
	{
		return Ok(await _db.Cinemas.Include((Cinema c) => c.Brand).ToListAsync());
	}

	[HttpGet("cinemas/{id}/movies")]
	public async Task<IActionResult> GetMoviesAtCinema(string id)
	{
		return Ok(await _db.Movies.Where((Movie m) => m.Status == "NowShowing").ToListAsync());
	}
}
