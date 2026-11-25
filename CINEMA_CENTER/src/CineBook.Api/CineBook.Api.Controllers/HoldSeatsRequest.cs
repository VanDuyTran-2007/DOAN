using System.Collections.Generic;

namespace CineBook.Api.Controllers;

public record HoldSeatsRequest(List<string> SeatIds, string? UserId);
