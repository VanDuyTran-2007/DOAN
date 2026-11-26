using System.Collections.Generic;

namespace CineBook.Api.Controllers;

public record CreateBookingDto(string ShowtimeId, string? HoldId, List<string> SeatIds, Dictionary<string, int>? Combos, string? UserId = null);
