using CineBook.Models;
using CineBook.Services.Interfaces;

namespace CineBook.Services.Mock;

// Các Mock service: dữ liệu cứng + Task.Delay giả độ trễ mạng.
// Backend team: viết ApiXxxService implement cùng interface rồi đổi đăng ký DI trong App.xaml.cs.

internal static class Net
{
    /// <summary>Giả lập độ trễ mạng.</summary>
    public static Task Delay(CancellationToken ct, int ms = 450) => Task.Delay(ms, ct);
}

public class MockMovieService : IMovieService
{
    public async Task<IReadOnlyList<Movie>> GetNowShowingAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 700); return MockData.Movies.Where(m => m.Status == MovieStatus.NowShowing).ToList(); }

    public async Task<IReadOnlyList<Movie>> GetComingSoonAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 600); return MockData.Movies.Where(m => m.Status == MovieStatus.ComingSoon).ToList(); }

    public async Task<IReadOnlyList<Movie>> GetHotAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 500); return MockData.Movies.Where(m => m.IsHot).ToList(); }

    public async Task<Movie?> GetByIdAsync(string id, CancellationToken ct = default)
    { await Net.Delay(ct, 300); return MockData.Movies.FirstOrDefault(m => m.Id == id); }

    public async Task<IReadOnlyList<Movie>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        await Net.Delay(ct, 250);
        return MockData.Movies.Where(m => m.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                       || m.OriginalTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<PagedResult<Movie>> GetByGenreAsync(MovieQuery q, CancellationToken ct = default)
    {
        await Net.Delay(ct, 400);
        IEnumerable<Movie> src = MockData.Movies;
        if (!string.IsNullOrEmpty(q.Genre)) src = src.Where(m => m.Genres.Contains(q.Genre));
        if (q.Status is { } st) src = src.Where(m => m.Status == st);
        // Mock: lọc định dạng dựa trên việc phim có suất ở định dạng đó (đơn giản hoá: theo hash).
        if (q.Format is { } f) src = src.Where(m => (MockData.StableHash(m.Id) & 3) == (int)f || f == MovieFormat.TwoD);
        src = q.Sort == MovieSort.Rating ? src.OrderByDescending(m => m.Rating) : src.OrderByDescending(m => m.ReleaseDate);
        var all = src.ToList();
        return new PagedResult<Movie>(all.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList(), all.Count, q.Page, q.PageSize);
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 100); return MockData.Genres; }
}

public class MockCinemaService : ICinemaService
{
    public async Task<IReadOnlyList<Brand>> GetBrandsAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 150); return MockData.Brands; }

    public async Task<IReadOnlyList<Cinema>> GetCinemasAsync(CinemaFilter f, CancellationToken ct = default)
    {
        await Net.Delay(ct, 400);
        return MockData.Cinemas.Where(c => (f.BrandId is null || c.BrandId == f.BrandId)
                                        && (f.City is null || c.City == f.City)
                                        && (f.District is null || c.District == f.District)).ToList();
    }

    public async Task<IReadOnlyList<Cinema>> GetNearbyAsync(double lat, double lng, double radiusKm, CancellationToken ct = default)
    {
        await Net.Delay(ct, 800);
        // NOTE-DB: server nên tính khoảng cách; mock tính Haversine để demo.
        foreach (var c in MockData.Cinemas) c.DistanceKm = Haversine(lat, lng, c.Latitude, c.Longitude);
        return MockData.Cinemas.Where(c => c.DistanceKm <= radiusKm).OrderBy(c => c.DistanceKm).ToList();
    }

    public async Task<IReadOnlyList<Movie>> GetMoviesAtCinemaAsync(string cinemaId, CancellationToken ct = default)
    {
        await Net.Delay(ct, 400);
        return MockData.Movies.Where(m => m.Status == MovieStatus.NowShowing && MockData.IsScreening(m.Id, cinemaId)).ToList();
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = (lat2 - lat1) * Math.PI / 180, dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

public class MockShowtimeService : IShowtimeService
{
    public async Task<IReadOnlyList<Showtime>> GetShowtimesAsync(string movieId, string? cinemaId, DateTime date, CancellationToken ct = default)
    {
        await Net.Delay(ct, 450);
        var cinemas = MockData.Cinemas.Where(c => (cinemaId is null || c.Id == cinemaId) && MockData.IsScreening(movieId, c.Id));
        return cinemas.SelectMany(c => MockData.ShowtimesFor(movieId, c.Id, date.Date))
                      .Where(s => s.StartTime > DateTimeOffset.UtcNow).ToList();
    }

    public async Task<Showtime?> GetByIdAsync(string showtimeId, CancellationToken ct = default)
    { await Net.Delay(ct, 150); return MockData.ParseShowtime(showtimeId); }
}

public class MockSeatService : ISeatService
{
    private static readonly string[] RowLabels = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"];

    public async Task<SeatMap> GetSeatMapAsync(string showtimeId, CancellationToken ct = default)
    {
        await Net.Delay(ct, 500);
        var st = MockData.ParseShowtime(showtimeId) ?? throw new InvalidOperationException("Suất chiếu không tồn tại");
        var rnd = new Random(MockData.StableHash(showtimeId));
        var rows = new List<SeatRow>();
        foreach (var (label, idx) in RowLabels.Select((l, i) => (l, i)))
        {
            var seats = new List<Seat>();
            bool couple = idx == RowLabels.Length - 1;
            bool vip = idx is >= 5 and <= 7;
            // Lưới 15 cột, cột 8 là lối đi giữa. Hàng cuối là ghế đôi (mỗi ghế rộng 2 cột).
            if (couple)
            {
                int[] starts = [1, 3, 5, 9, 11, 13];
                for (int n = 0; n < starts.Length; n++)
                    seats.Add(Make(st, label, n + 1, SeatType.Couple, starts[n], 2, rnd));
            }
            else
            {
                int num = 1;
                for (int col = 1; col <= 15; col++)
                {
                    if (col == 8) continue;
                    // Hàng A bỏ 2 ghế đầu hàng để minh hoạ khoảng trống.
                    if (idx == 0 && (col == 1 || col == 15)) continue;
                    seats.Add(Make(st, label, num++, vip ? SeatType.Vip : SeatType.Standard, col, 1, rnd));
                }
            }
            rows.Add(new SeatRow { Label = label, Seats = seats });
        }
        return new SeatMap { ShowtimeId = showtimeId, AuditoriumName = st.AuditoriumName, TotalColumns = 15, Rows = rows };
    }

    /// <summary>Giá ghế theo hàng (A-E thường, F-H VIP, J ghế đôi). Backend thật tính theo loại ghế/định dạng/ngày.</summary>
    public static decimal PriceFor(Showtime st, string seatLabel)
        => seatLabel[0] switch { 'J' => st.BasePrice * 2 + 10000, >= 'F' and <= 'H' => st.BasePrice + 25000, _ => st.BasePrice };

    private static Seat Make(Showtime st, string row, int num, SeatType type, int col, int span, Random rnd)
    {
        decimal price = PriceFor(st, row);
        int roll = rnd.Next(100);
        var status = roll < 22 ? SeatStatus.Booked : roll < 27 ? SeatStatus.Holding : roll < 29 ? SeatStatus.Unavailable : SeatStatus.Available;
        return new Seat { Id = $"{st.AuditoriumId}-{row}{num}", Row = row, Number = num, Type = type, Status = status, Price = price, GridColumn = col, ColumnSpan = span };
    }

    public async Task<SeatHold> HoldSeatsAsync(string showtimeId, IReadOnlyList<string> seatIds, CancellationToken ct = default)
    {
        await Net.Delay(ct, 350);
        return new SeatHold { HoldId = Guid.NewGuid().ToString("N"), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10) };
    }

    public async Task ReleaseSeatsAsync(string holdId, CancellationToken ct = default) => await Net.Delay(ct, 100);
}

public class MockAuthService : IAuthService
{
    public User? CurrentUser { get; private set; }
    public event EventHandler? AuthStateChanged;

    public async Task<AuthResult> SignInAsync(string identity, string password, CancellationToken ct = default)
    {
        await Net.Delay(ct, 900);
        // Mock: mật khẩu sai khi là "wrong-password" để demo trạng thái lỗi.
        if (password == "wrong-password") return new AuthResult(false, "Email/SĐT hoặc mật khẩu không đúng", null);
        CurrentUser = new User { Id = "u1", FullName = "Nguyễn Văn An", Email = identity.Contains('@') ? identity : "an@example.com",
                                 Phone = identity.Contains('@') ? "0900000000" : identity, MemberTier = MemberTier.Silver };
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
        return new AuthResult(true, null, CurrentUser);
    }

    public async Task<AuthResult> SignUpAsync(SignUpRequest r, CancellationToken ct = default)
    {
        await Net.Delay(ct, 1000);
        CurrentUser = new User { Id = "u2", FullName = r.FullName, Email = r.Email, Phone = r.Phone, MemberTier = MemberTier.Standard };
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
        return new AuthResult(true, null, CurrentUser);
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        await Net.Delay(ct, 150);
        CurrentUser = null;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }
}

public class MockBookingService : IBookingService
{
    private readonly List<Booking> _mine = [];
    private readonly Dictionary<string, Booking> _pending = [];
    private readonly IShowtimeService _showtimes;
    public MockBookingService(IShowtimeService showtimes) => _showtimes = showtimes;

    public async Task<IReadOnlyList<Combo>> GetCombosAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 350); return MockData.Combos; }

    public async Task<Booking> CreateBookingAsync(CreateBookingRequest r, CancellationToken ct = default)
    {
        await Net.Delay(ct, 500);
        var st = await _showtimes.GetByIdAsync(r.ShowtimeId, ct) ?? throw new InvalidOperationException("Suất chiếu không tồn tại");
        var movie = MockData.Movies.First(m => m.Id == st.MovieId);
        var cinema = MockData.Cinemas.First(c => c.Id == st.CinemaId);
        // Mock: không có giá thật ở đây; ước lượng từ BasePrice. Backend phải tính lại.
        decimal total = r.SeatIds.Sum(id => MockSeatService.PriceFor(st, id[(id.LastIndexOf('-') + 1)..]))
                      + r.Combos.Sum(kv => MockData.Combos.First(c => c.Id == kv.Key).Price * kv.Value);
        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString("N"), Code = "CB" + Random.Shared.Next(100000, 999999),
            MovieTitle = movie.Title, CinemaName = cinema.Name, AuditoriumName = st.AuditoriumName, StartTime = st.StartTime,
            SeatLabels = r.SeatIds.Select(s => s[(s.LastIndexOf('-') + 1)..]).ToList(), TotalAmount = total, Status = BookingStatus.Pending
        };
        _pending[booking.Id] = booking;
        return booking;
    }

    public async Task<PaymentResult> ConfirmPaymentAsync(string bookingId, PaymentMethod method, CancellationToken ct = default)
    {
        await Net.Delay(ct, 2000); // giả 2 giây chờ cổng thanh toán
        if (!_pending.TryGetValue(bookingId, out var b)) return new PaymentResult(false, "Đơn đặt vé không tồn tại hoặc đã hết hạn", null);
        var paid = new Booking
        {
            Id = b.Id, Code = b.Code, MovieTitle = b.MovieTitle, CinemaName = b.CinemaName, AuditoriumName = b.AuditoriumName,
            StartTime = b.StartTime, SeatLabels = b.SeatLabels, TotalAmount = b.TotalAmount, Status = BookingStatus.Paid
        };
        _pending.Remove(bookingId);
        _mine.Insert(0, paid);
        return new PaymentResult(true, null, paid);
    }

    public async Task<IReadOnlyList<Booking>> GetMyTicketsAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 400); return _mine; }

    /// <summary>Chỉ mock: lưu vé đã thanh toán để trang "Vé của tôi" hiển thị.</summary>
    public void AddPaid(Booking b) => _mine.Add(b);
}

public class MockLocationService : ILocationService
{
    public IReadOnlyList<GeoLocation> Cities => MockData.Cities;

    public async Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default)
    {
        await Net.Delay(ct, 600);
        // Mock: luôn trả toạ độ TP.HCM. Thực tế: dùng Windows.Devices.Geolocation; trả null nếu bị từ chối.
        return MockData.Cities[0];
    }
}
