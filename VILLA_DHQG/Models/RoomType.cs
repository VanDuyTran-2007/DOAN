namespace DOAN.Models;

/// <summary>Một lượt đặt phòng (khoảng [From, To) — To là ngày trả phòng).</summary>
public record Booking(DateTime From, DateTime To);

/// <summary>Một phòng cụ thể (ví dụ P.301) thuộc một hạng phòng.</summary>
public class Room
{
    public string Number { get; init; } = "";
    public int Floor { get; init; }
    public List<Booking> Bookings { get; init; } = [];

    /// <summary>Phòng có trống trong suốt khoảng [from, to) không.</summary>
    public bool IsFree(DateTime from, DateTime to) =>
        !Bookings.Any(b => b.From < to && from < b.To);

    public bool IsFreeOn(DateTime day) => IsFree(day.Date, day.Date.AddDays(1));
}

/// <summary>Hạng phòng (Standard, Deluxe, Villa...).</summary>
public class RoomType
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string? Highlight { get; init; }
    public string ShortDescription { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Price { get; init; }
    public double Area { get; init; }
    public int Beds { get; init; }
    public string BedType { get; init; } = "";
    public int MaxAdults { get; init; }
    public int MaxChildren { get; init; }
    public string View { get; init; } = "";
    public List<string> Images { get; init; } = [];
    public List<string> Amenities { get; init; } = [];
    public List<Room> Rooms { get; init; } = [];

    public string Cover => Images.FirstOrDefault() ?? "";
    public int TotalRooms => Rooms.Count;
    public int MaxGuests => MaxAdults + MaxChildren;
    public bool HasHighlight => !string.IsNullOrEmpty(Highlight);

    public string PriceText => $"{Price:N0} đ";
    public string AreaText => $"{Area:0} m²";
    public string BedsText => $"{Beds} giường";
    public string GuestsText => MaxChildren > 0
        ? $"{MaxAdults} người lớn + {MaxChildren} trẻ em"
        : $"{MaxAdults} người lớn";
    public string ShortGuestsText => $"Tối đa {MaxGuests} khách";
    public IEnumerable<string> TopAmenities => Amenities.Take(4);

    public int CountAvailable(DateTime from, DateTime to) => Rooms.Count(r => r.IsFree(from, to));
}
