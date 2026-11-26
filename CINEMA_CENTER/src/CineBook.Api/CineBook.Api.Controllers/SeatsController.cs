using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Controllers;

[ApiController]
[Route("api")]
public class SeatsController : ControllerBase
{
	private readonly CineBookDbContext _db;

	public SeatsController(CineBookDbContext db)
	{
		_db = db;
	}

	[HttpGet("showtimes/{showtimeId}/seats")]
	public async Task<IActionResult> GetSeatMap(string showtimeId)
	{
		Showtime showtime = await _db.Showtimes.Include((Showtime s) => s.Auditorium).FirstOrDefaultAsync((Showtime s) => s.Id == showtimeId);
		if (showtime == null)
		{
			return NotFound("Suất chiếu không tồn tại.");
		}
		DateTime now = DateTime.UtcNow;
		List<Seat> seats = await (from s in _db.Seats
			where s.AuditoriumId == showtime.AuditoriumId
			orderby s.Row, s.Number
			select s).ToListAsync();
		HashSet<string> bookedIds = await (from bs in _db.BookingSeats
			where bs.ShowtimeId == showtimeId && bs.IsActive
			select bs.SeatId).ToHashSetAsync();
		HashSet<string> heldIds = await (from h in _db.SeatHolds
			join hs in _db.SeatHoldSeats on h.HoldId equals hs.HoldId
			where h.ShowtimeId == showtimeId && h.ExpiresAtUtc > now
			select hs.SeatId).ToHashSetAsync();
		var rows = (from s in seats
			group s by s.Row into g
			orderby g.Key
			select new
			{
				Label = g.Key,
				Seats = g.OrderBy((Seat s) => s.Number).Select((Seat s) =>
				{
					string text = (s.Type.Equals("Vip", StringComparison.OrdinalIgnoreCase) ? "Vip" : ((s.Type.Equals("Couple", StringComparison.OrdinalIgnoreCase) || s.Type.Equals("Sweetbox", StringComparison.OrdinalIgnoreCase)) ? "Couple" : "Standard"));
					string status = ((!s.IsAvailable) ? "Unavailable" : (bookedIds.Contains(s.Id) ? "Booked" : (heldIds.Contains(s.Id) ? "Holding" : "Available")));
					decimal price = ((text == "Vip") ? (showtime.BasePrice + 25000m) : ((text == "Couple") ? (showtime.BasePrice * 2m + 10000m) : showtime.BasePrice));
					return new
					{
						Id = s.Id,
						Row = s.Row,
						Number = s.Number,
						Type = text,
						Status = status,
						Price = price,
						GridColumn = s.GridColumn,
						ColumnSpan = s.ColumnSpan
					};
				}).ToList()
			}).ToList();
		return Ok(new
		{
			ShowtimeId = showtimeId,
			AuditoriumName = (showtime.Auditorium?.Name ?? "Phòng 1"),
			TotalColumns = (showtime.Auditorium?.TotalColumns ?? 15),
			Rows = rows
		});
	}

	[HttpPost("showtimes/{showtimeId}/holds")]
	public async Task<IActionResult> HoldSeats(string showtimeId, [FromBody] HoldSeatsRequest req)
	{
		if (req.SeatIds == null || req.SeatIds.Count == 0)
		{
			return BadRequest("Chưa chọn ghế nào.");
		}
		if (req.SeatIds.Count > 8)
		{
			return UnprocessableEntity("Chỉ được chọn tối đa 8 ghế mỗi lần.");
		}
		DateTime now = DateTime.UtcNow;
		List<string> expiredIds = await (from h in _db.SeatHolds
			where h.ExpiresAtUtc <= now
			select h.HoldId).ToListAsync();
		if (expiredIds.Count > 0)
		{
			await _db.SeatHoldSeats.Where((SeatHoldSeat hs) => expiredIds.Contains(hs.HoldId)).ExecuteDeleteAsync();
			await _db.SeatHolds.Where((SeatHold h) => expiredIds.Contains(h.HoldId)).ExecuteDeleteAsync();
		}
		if (await _db.BookingSeats.AnyAsync((BookingSeat bs) => bs.ShowtimeId == showtimeId && bs.IsActive && req.SeatIds.Contains(bs.SeatId)) | await (from h in _db.SeatHolds
			join hs in _db.SeatHoldSeats on h.HoldId equals hs.HoldId
			where h.ShowtimeId == showtimeId && h.ExpiresAtUtc > now && req.SeatIds.Contains(hs.SeatId)
			select hs.SeatId).AnyAsync())
		{
			return Conflict("Một hoặc nhiều ghế bạn chọn vừa có người khác giữ hoặc đặt.");
		}
		string holdId = "hold-" + Guid.NewGuid().ToString("N").Substring(0, 12);
		DateTime expiresAtUtc = now.AddMinutes(10.0);
		try
		{
			_db.SeatHolds.Add(new SeatHold
			{
				HoldId = holdId,
				ShowtimeId = showtimeId,
				UserId = req.UserId,
				CreatedAtUtc = now,
				ExpiresAtUtc = expiresAtUtc
			});
			await _db.SaveChangesAsync();
			foreach (string seatId in req.SeatIds.Distinct())
			{
				_db.SeatHoldSeats.Add(new SeatHoldSeat
				{
					HoldId = holdId,
					ShowtimeId = showtimeId,
					SeatId = seatId
				});
			}
			await _db.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			return BadRequest(ex.InnerException?.Message ?? ex.Message);
		}
		return Ok(new
		{
			HoldId = holdId,
			ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc))
		});
	}

	[HttpDelete("holds/{holdId}")]
	public async Task<IActionResult> ReleaseHold(string holdId)
	{
		await _db.SeatHoldSeats.Where((SeatHoldSeat hs) => hs.HoldId == holdId).ExecuteDeleteAsync();
		await _db.SeatHolds.Where((SeatHold h) => h.HoldId == holdId).ExecuteDeleteAsync();
		return NoContent();
	}
}
