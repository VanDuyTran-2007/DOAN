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
public class BookingsController : ControllerBase
{
	private readonly CineBookDbContext _db;

	private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

	public BookingsController(CineBookDbContext db)
	{
		_db = db;
	}

	[HttpGet("combos")]
	public async Task<IActionResult> GetCombos()
	{
		return Ok(await _db.Combos.OrderBy((Combo c) => c.Price).ToListAsync());
	}

	[HttpPost("bookings")]
	public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto req)
	{
		if (req.SeatIds == null || req.SeatIds.Count == 0)
		{
			return BadRequest("Danh sách ghế trống.");
		}
		Showtime showtime = await _db.Showtimes.Include((Showtime showtime2) => showtime2.Movie).Include((Showtime showtime2) => showtime2.Auditorium).ThenInclude((Auditorium a) => a.Cinema)
			.FirstOrDefaultAsync((Showtime showtime2) => showtime2.Id == req.ShowtimeId);
		if (showtime == null)
		{
			return NotFound("Suất chiếu không tồn tại.");
		}
		DateTime now = DateTime.UtcNow;
		if (await _db.BookingSeats.AnyAsync((BookingSeat bs) => bs.ShowtimeId == req.ShowtimeId && bs.IsActive && req.SeatIds.Contains(bs.SeatId)))
		{
			return Conflict("Một hoặc nhiều ghế đã được đặt trước đó.");
		}
		List<Seat> seats = await _db.Seats.Where((Seat seat) => req.SeatIds.Contains(seat.Id)).ToListAsync();
		if (seats.Count != req.SeatIds.Distinct().Count())
		{
			return UnprocessableEntity("Mã ghế không hợp lệ.");
		}
		decimal seatTotal = 0m;
		Dictionary<string, decimal> seatPrices = new Dictionary<string, decimal>();
		foreach (Seat s in seats)
		{
			decimal price;
			if (s.Type.Equals("Vip", StringComparison.OrdinalIgnoreCase))
			{
				price = showtime.BasePrice + 25000m;
			}
			else
			{
				price = ((s.Type.Equals("Couple", StringComparison.OrdinalIgnoreCase) || s.Type.Equals("Sweetbox", StringComparison.OrdinalIgnoreCase)) ? (showtime.BasePrice * 2m + 10000m) : showtime.BasePrice);
			}
			seatPrices[s.Id] = price;
			seatTotal += price;
		}
		decimal comboTotal = 0m;
		List<(Combo combo, int qty)> validCombos = new List<(Combo, int)>();
		Dictionary<string, int> combos = req.Combos;
		if (combos != null && combos.Count > 0)
		{
			List<string> comboIds = (from kv in req.Combos
				where kv.Value > 0
				select kv.Key).ToList();
			Dictionary<string, Combo> dbCombos = await _db.Combos.Where((Combo combo) => comboIds.Contains(combo.Id)).ToDictionaryAsync((Combo combo) => combo.Id);
			foreach (var (comboId, qty) in req.Combos)
			{
				if (qty > 0 && dbCombos.TryGetValue(comboId, out var c))
				{
					comboTotal += c.Price * (decimal)qty;
					validCombos.Add((c, qty));
					c = null;
				}
			}
		}
		Booking booking = new Booking
		{
			Id = Guid.NewGuid().ToString("N"),
			Code = "CB" + Random.Shared.Next(100000, 999999),
			UserId = (string.IsNullOrWhiteSpace(req.UserId) ? null : req.UserId),
			ShowtimeId = req.ShowtimeId,
			Status = "Pending",
			TotalAmount = seatTotal + comboTotal,
			IdempotencyKey = (string.IsNullOrWhiteSpace(req.HoldId) ? Guid.NewGuid().ToString("N") : req.HoldId),
			CreatedAtUtc = now,
			ExpiresAtUtc = now.AddMinutes(10.0)
		};
		_db.Bookings.Add(booking);
		await _db.SaveChangesAsync();
		foreach (Seat s2 in seats)
		{
			_db.BookingSeats.Add(new BookingSeat
			{
				BookingId = booking.Id,
				ShowtimeId = req.ShowtimeId,
				SeatId = s2.Id,
				Price = seatPrices[s2.Id],
				IsActive = false
			});
		}
		foreach (var (c2, qty2) in validCombos)
		{
			_db.BookingCombos.Add(new BookingCombo
			{
				BookingId = booking.Id,
				ComboId = c2.Id,
				Qty = qty2,
				UnitPrice = c2.Price
			});
		}
		await _db.SaveChangesAsync();
		return Ok(MapBookingDto(booking, showtime, seats));
	}

	[HttpPost("bookings/{bookingId}/pay")]
	public async Task<IActionResult> ConfirmPayment(string bookingId, [FromBody] ConfirmPaymentDto req)
	{
		Booking booking = await _db.Bookings.Include((Booking b) => b.Showtime).ThenInclude((Showtime s) => s.Movie).Include((Booking b) => b.Showtime)
			.ThenInclude((Showtime s) => s.Auditorium)
			.ThenInclude((Auditorium a) => a.Cinema)
			.FirstOrDefaultAsync((Booking b) => b.Id == bookingId);
		if (booking == null || booking.Showtime == null)
		{
			return Ok(new
			{
				Success = false,
				Error = "Đơn đặt vé không tồn tại hoặc đã hết hạn",
				Booking = (object)null
			});
		}
		List<BookingSeat> bookingSeats = await _db.BookingSeats.Where((BookingSeat bookingSeat) => bookingSeat.BookingId == bookingId).ToListAsync();
		List<string> seatIds = bookingSeats.Select((BookingSeat bookingSeat) => bookingSeat.SeatId).ToList();
		if (booking.Status != "Paid")
		{
			if (await _db.BookingSeats.AnyAsync((BookingSeat bookingSeat) => bookingSeat.ShowtimeId == booking.ShowtimeId && bookingSeat.BookingId != bookingId && bookingSeat.IsActive && seatIds.Contains(bookingSeat.SeatId)))
			{
				return Ok(new
				{
					Success = false,
					Error = "Ghế đã có người khác thanh toán trước",
					Booking = (object)null
				});
			}
			booking.Status = "Paid";
			foreach (BookingSeat bs in bookingSeats)
			{
				bs.IsActive = true;
			}
			await _db.SeatHoldSeats.Where((SeatHoldSeat hs) => hs.ShowtimeId == booking.ShowtimeId && seatIds.Contains(hs.SeatId)).ExecuteDeleteAsync();
			if (!string.IsNullOrWhiteSpace(booking.IdempotencyKey))
			{
				await _db.SeatHolds.Where((SeatHold h) => h.HoldId == booking.IdempotencyKey).ExecuteDeleteAsync();
			}
			await _db.SaveChangesAsync();
		}
		return Ok(new
		{
			Success = true,
			Error = (string)null,
			Booking = MapBookingDto(seats: await _db.Seats.Where((Seat s) => seatIds.Contains(s.Id)).ToListAsync(), b: booking, st: booking.Showtime)
		});
	}

	[HttpGet("me/bookings")]
	public async Task<IActionResult> GetMyTickets([FromQuery] string? userId, [FromQuery] string? ids)
	{
		List<string> idList = (string.IsNullOrWhiteSpace(ids) ? new List<string>() : ids.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
		IQueryable<Booking> query = from b in _db.Bookings.Include((Booking b) => b.Showtime).ThenInclude((Showtime s) => s.Movie).Include((Booking b) => b.Showtime)
				.ThenInclude((Showtime s) => s.Auditorium)
				.ThenInclude((Auditorium a) => a.Cinema)
			where b.Status == "Paid"
			select b;
		if (!string.IsNullOrWhiteSpace(userId) && idList.Count > 0)
		{
			query = query.Where((Booking b) => b.UserId == userId || idList.Contains(b.Id));
		}
		else if (!string.IsNullOrWhiteSpace(userId))
		{
			query = query.Where((Booking b) => b.UserId == userId);
		}
		else
		{
			if (idList.Count <= 0)
			{
				return Ok(Array.Empty<object>());
			}
			query = query.Where((Booking b) => idList.Contains(b.Id));
		}
		List<Booking> bookings = await query.OrderByDescending((Booking b) => b.CreatedAtUtc).ToListAsync();
		List<string> bookingIds = bookings.Select((Booking b) => b.Id).ToList();
		Dictionary<string, List<Seat>> seatsByBooking = (from x in await (from bs in _db.BookingSeats
				join s in _db.Seats on bs.SeatId equals s.Id
				where bookingIds.Contains(bs.BookingId)
				select new { bs.BookingId, s }).ToListAsync()
			group x by x.BookingId).ToDictionary(g => g.Key, g => g.Select(x => x.s).ToList());
		IEnumerable<object> result = from b in bookings
			where b.Showtime != null
			select MapBookingDto(b, b.Showtime, seatsByBooking.GetValueOrDefault(b.Id, new List<Seat>()));
		return Ok(result);
	}

	private static object MapBookingDto(Booking b, Showtime st, List<Seat> seats)
	{
		return new
		{
			Id = b.Id,
			Code = b.Code,
			MovieTitle = (st.Movie?.Title ?? ""),
			CinemaName = (st.Auditorium?.Cinema?.Name ?? ""),
			AuditoriumName = (st.Auditorium?.Name ?? ""),
			StartTime = new DateTimeOffset(DateTime.SpecifyKind(st.StartTimeUtc, DateTimeKind.Utc)).ToOffset(VnOffset),
			SeatLabels = (from s in seats
				orderby s.Row, s.Number
				select $"{s.Row}{s.Number}").ToList(),
			TotalAmount = b.TotalAmount,
			Status = b.Status
		};
	}
}
