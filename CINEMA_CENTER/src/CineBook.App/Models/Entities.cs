namespace CineBook.Models;

// NOTE-DB: bảng Brands(Id PK, Name unique, LogoUrl, ThemeColorHex).
// WARNING-BACKEND: Id đồng bộ từ rạp ngoài định danh theo (BrandId + ExternalId). Mỗi hãng có API riêng → dùng Adapter pattern (ICinemaProvider cho CGV, Lotte...) rồi đồng bộ về DB chung.
public class Brand
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string LogoUrl { get; init; } = "";
    /// <summary>Màu badge theo hãng, ví dụ "#C8102E".</summary>
    public required string ThemeColorHex { get; init; }
}

// NOTE-DB: bảng Cinemas(Id PK, BrandId FK, Name, Address, City, District, Latitude, Longitude); index (City, BrandId); index không gian trên (Latitude, Longitude).
public class Cinema
{
    public required string Id { get; init; }
    public required string BrandId { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string District { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    // NOTE-DB: DistanceKm KHÔNG lưu DB; server tính theo toạ độ người dùng (Haversine hoặc kiểu geography).
    public double? DistanceKm { get; set; }

    /// <summary>Do service gắn vào để UI hiện badge; backend có thể trả kèm hoặc client tự join theo BrandId.</summary>
    public Brand? Brand { get; set; }

    public string DistanceText => DistanceKm is null ? "" : $"{DistanceKm:0.0} km";
}

// NOTE-DB: bảng Movies(Id PK, Title, ...); bảng MovieGenres(MovieId, Genre) hoặc Genres + bảng nối nhiều-nhiều.
// NOTE-DB: PosterUrl/BackdropUrl chỉ lưu URL (CDN), không lưu binary trong DB.
public class Movie
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string OriginalTitle { get; init; } = "";
    public string PosterUrl { get; init; } = "";
    public string BackdropUrl { get; init; } = "";
    public string TrailerUrl { get; init; } = "";
    public IReadOnlyList<string> Genres { get; init; } = [];
    public int DurationMinutes { get; init; }
    public AgeRating AgeRating { get; init; }
    public DateTime ReleaseDate { get; init; }
    public double Rating { get; init; }
    public MovieStatus Status { get; init; }
    public bool IsHot { get; init; }
    public string Description { get; init; } = "";
    public string Director { get; init; } = "";
    public IReadOnlyList<string> Cast { get; init; } = [];

    /// <summary>Chỉ dùng cho poster placeholder khi chưa có PosterUrl thật (không cần lưu DB).</summary>
    public string PlaceholderColorHex { get; init; } = "#800020";

    public string GenresText => string.Join(", ", Genres);
    public string CastText => string.Join(", ", Cast);
    public string DurationText => $"{DurationMinutes} phút";
    public bool HasRating => Rating > 0;
    public string RatingText => Rating.ToString("0.0");
    public string ReleaseText => ReleaseDate.ToString("dd/MM/yyyy");
}

// NOTE-DB: bảng Showtimes(Id PK, MovieId FK, AuditoriumId FK, StartTimeUtc, Format, Language, BasePrice); index (MovieId, StartTimeUtc), (AuditoriumId, StartTimeUtc).
// WARNING-BACKEND: kiểm tra không trùng giờ trong cùng một phòng chiếu.
// NOTE-DB: lưu thời gian dạng UTC; client hiển thị giờ Việt Nam (UTC+7).
public class Showtime
{
    public required string Id { get; init; }
    public required string MovieId { get; init; }
    public required string CinemaId { get; init; }
    public required string AuditoriumId { get; init; }
    public string AuditoriumName { get; init; } = "";
    public DateTimeOffset StartTime { get; init; }
    public MovieFormat Format { get; init; }
    public SubtitleLanguage Language { get; init; }
    public decimal BasePrice { get; init; }

    public string TimeText => StartTime.ToString("HH:mm");
    public string FormatText => $"{Labels.Format(Format)} {Labels.Language(Language)}";
}

// NOTE-DB: bảng Seats unique (AuditoriumId, Row, Number).
public class Seat
{
    public required string Id { get; init; }
    public required string Row { get; init; }
    public int Number { get; init; }
    public SeatType Type { get; init; }
    public SeatStatus Status { get; set; }
    // WARNING-BACKEND: giá ghế chỉ để hiển thị; server phải tính lại giá khi tạo booking.
    public decimal Price { get; init; }
    /// <summary>Cột bắt đầu (từ 1). Lối đi = những cột bị bỏ trống.</summary>
    public int GridColumn { get; init; }
    /// <summary>Ghế đôi chiếm 2 cột.</summary>
    public int ColumnSpan { get; init; } = 1;
    public string Label => $"{Row}{Number}";
}

public class SeatRow
{
    public required string Label { get; init; }
    public required IReadOnlyList<Seat> Seats { get; init; }
}

public class SeatMap
{
    public required string ShowtimeId { get; init; }
    public required string AuditoriumName { get; init; }
    public int TotalColumns { get; init; }
    public required IReadOnlyList<SeatRow> Rows { get; init; }
}

public class SeatHold
{
    public required string HoldId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}

public class User
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string Phone { get; init; } = "";
    public string AvatarUrl { get; init; } = "";
    public MemberTier MemberTier { get; init; }
    public string Initials => string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeLast(2).Select(w => char.ToUpper(w[0])));
}

public class Combo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public decimal Price { get; init; }
    public string ImageUrl { get; init; } = "";
}

// NOTE-DB: Bookings(Id, Code unique, UserId FK, ShowtimeId FK, Status, TotalAmount, CreatedAtUtc, ExpiresAtUtc), BookingSeats(BookingId, SeatId, Price), BookingCombos(BookingId, ComboId, Qty, UnitPrice).
public class Booking
{
    public required string Id { get; init; }
    public required string Code { get; init; }
    public required string MovieTitle { get; init; }
    public required string CinemaName { get; init; }
    public required string AuditoriumName { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public required IReadOnlyList<string> SeatLabels { get; init; }
    public decimal TotalAmount { get; init; }
    public BookingStatus Status { get; init; }
    public string SeatsText => string.Join(", ", SeatLabels);
}

// ---- Request / response DTO ----
public record CinemaFilter(string? BrandId = null, string? City = null, string? District = null);
public record MovieQuery(string? Genre = null, MovieStatus? Status = null, MovieFormat? Format = null,
                         MovieSort Sort = MovieSort.Newest, int Page = 1, int PageSize = 12);
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public bool HasMore => Page * PageSize < TotalCount;
}
public record GeoLocation(double Latitude, double Longitude, string City);
public record AuthResult(bool Success, string? Error, User? User);
public record SignUpRequest(string FullName, string Email, string Phone, DateTime BirthDate, string Password);
public record CreateBookingRequest(string ShowtimeId, string HoldId, IReadOnlyList<string> SeatIds, IReadOnlyDictionary<string, int> Combos);
public record PaymentResult(bool Success, string? Error, Booking? Booking);

/// <summary>Chuỗi hiển thị tiếng Việt cho enum.</summary>
public static class Labels
{
    public static string Format(MovieFormat f) => f switch { MovieFormat.TwoD => "2D", MovieFormat.ThreeD => "3D", MovieFormat.IMAX => "IMAX", _ => "4DX" };
    public static string Language(SubtitleLanguage l) => l == SubtitleLanguage.Subtitled ? "Phụ đề" : "Lồng tiếng";
    public static string Age(AgeRating a) => a.ToString();
    public static string SeatType(SeatType t) => t switch { Models.SeatType.Vip => "VIP", Models.SeatType.Couple => "Ghế đôi", _ => "Thường" };
    public static string Money(decimal v) => v.ToString("#,##0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
}
