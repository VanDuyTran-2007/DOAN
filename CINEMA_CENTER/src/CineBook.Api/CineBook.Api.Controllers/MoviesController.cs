using System.Threading.Tasks;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
	private readonly CineBookDbContext _db;

	public MoviesController(CineBookDbContext db)
	{
		_db = db;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		return Ok(await _db.Movies.ToListAsync());
	}
}
