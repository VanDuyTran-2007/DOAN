using System.IO;
using System.Text.Json;
using CineBook.Models;
using CineBook.Services.Interfaces;

namespace CineBook.Services.Mock;

internal static class Net
{
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
    private readonly Dictionary<string, HashSet<string>> _bookedByShowtime = [];

    public void MarkAsBooked(string showtimeId, IEnumerable<string> seatIds)
    {
        if (!_bookedByShowtime.TryGetValue(showtimeId, out var set))
        {
            set = [];
            _bookedByShowtime[showtimeId] = set;
        }
        foreach (var id in seatIds) set.Add(id);
    }

    public async Task<SeatMap> GetSeatMapAsync(string showtimeId, CancellationToken ct = default)
    {
        await Net.Delay(ct, 500);
        var st = MockData.ParseShowtime(showtimeId) ?? throw new InvalidOperationException("Suất chiếu không tồn tại");
        var rnd = new Random(MockData.StableHash(showtimeId));
        _bookedByShowtime.TryGetValue(showtimeId, out var bookedIds);

        var rows = new List<SeatRow>();
        foreach (var (label, idx) in RowLabels.Select((l, i) => (l, i)))
        {
            var seats = new List<Seat>();
            bool couple = idx == RowLabels.Length - 1;
            bool vip = idx is >= 5 and <= 7;
            if (couple)
            {
                int[] starts = [1, 3, 5, 9, 11, 13];
                for (int n = 0; n < starts.Length; n++)
                    seats.Add(Make(st, label, n + 1, SeatType.Couple, starts[n], 2, rnd, bookedIds));
            }
            else
            {
                int num = 1;
                for (int col = 1; col <= 15; col++)
                {
                    if (col == 8) continue;
                    if (idx == 0 && (col == 1 || col == 15)) continue;
                    seats.Add(Make(st, label, num++, vip ? SeatType.Vip : SeatType.Standard, col, 1, rnd, bookedIds));
                }
            }
            rows.Add(new SeatRow { Label = label, Seats = seats });
        }
        return new SeatMap { ShowtimeId = showtimeId, AuditoriumName = st.AuditoriumName, TotalColumns = 15, Rows = rows };
    }

    public static decimal PriceFor(Showtime st, string seatLabel)
        => seatLabel[0] switch { 'J' => st.BasePrice * 2 + 10000, >= 'F' and <= 'H' => st.BasePrice + 25000, _ => st.BasePrice };

    private static Seat Make(Showtime st, string row, int num, SeatType type, int col, int span, Random rnd, HashSet<string>? bookedIds)
    {
        decimal price = PriceFor(st, row);
        string seatId = $"{st.AuditoriumId}-{row}{num}";
        int roll = rnd.Next(100);
        var status = (bookedIds is not null && bookedIds.Contains(seatId))
            ? SeatStatus.Booked
            : roll < 22 ? SeatStatus.Booked : roll < 27 ? SeatStatus.Holding : roll < 29 ? SeatStatus.Unavailable : SeatStatus.Available;

        return new Seat { Id = seatId, Row = row, Number = num, Type = type, Status = status, Price = price, GridColumn = col, ColumnSpan = span };
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
    private record SavedAccount(User User, string Password);

    private static readonly string StorePath = Path.Combine(AppContext.BaseDirectory, "mock_users.json");
    private static readonly List<SavedAccount> _users = LoadUsers();

    public User? CurrentUser { get; private set; }
    public event EventHandler? AuthStateChanged;

    private static List<SavedAccount> LoadUsers()
    {
        try
        {
            if (File.Exists(StorePath))
            {
                var json = File.ReadAllText(StorePath);
                var list = JsonSerializer.Deserialize<List<SavedAccount>>(json);
                if (list is { Count: > 0 }) return list;
            }
        }
        catch { }

        return
        [
            new SavedAccount(
                new User { Id = "u1", FullName = "Nguyễn Văn An", Email = "an@example.com", Phone = "0900000000", MemberTier = MemberTier.Silver },
                "123456")
        ];
    }

    private static void SaveUsers()
    {
        try
        {
            var json = JsonSerializer.Serialize(_users);
            File.WriteAllText(StorePath, json);
        }
        catch { }
    }

    public async Task<AuthResult> SignInAsync(string identity, string password, CancellationToken ct = default)
    {
        await Net.Delay(ct, 600);
        var key = identity.Trim();

        var matched = _users.FirstOrDefault(x =>
            (x.User.Email.Equals(key, StringComparison.OrdinalIgnoreCase) || x.User.Phone == key)
            && x.Password == password);

        if (matched is null)
            return new AuthResult(false, "Email/SĐT hoặc mật khẩu không đúng", null);

        CurrentUser = matched.User;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
        return new AuthResult(true, null, CurrentUser);
    }

    public async Task<AuthResult> SignUpAsync(SignUpRequest r, CancellationToken ct = default)
    {
        await Net.Delay(ct, 700);
        if (_users.Any(x => x.User.Email.Equals(r.Email.Trim(), StringComparison.OrdinalIgnoreCase) || x.User.Phone == r.Phone.Trim()))
            return new AuthResult(false, "Email hoặc SĐT đã được đăng ký", null);

        var newUser = new User
        {
            Id = Guid.NewGuid().ToString("N"),
            FullName = r.FullName.Trim(),
            Email = r.Email.Trim(),
            Phone = r.Phone.Trim(),
            MemberTier = MemberTier.Standard
        };
        _users.Add(new SavedAccount(newUser, r.Password));
        SaveUsers();

        CurrentUser = newUser;
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
    private readonly Dictionary<string, (Booking Booking, string ShowtimeId, IReadOnlyList<string> SeatIds)> _pending = [];
    private readonly IShowtimeService _showtimes;
    private readonly ISeatService _seats;

    public MockBookingService(IShowtimeService showtimes, ISeatService seats)
    {
        _showtimes = showtimes;
        _seats = seats;
    }

    public async Task<IReadOnlyList<Combo>> GetCombosAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 350); return MockData.Combos; }

    public async Task<Booking> CreateBookingAsync(CreateBookingRequest r, CancellationToken ct = default)
    {
        await Net.Delay(ct, 500);
        var st = await _showtimes.GetByIdAsync(r.ShowtimeId, ct) ?? throw new InvalidOperationException("Suất chiếu không tồn tại");
        var movie = MockData.Movies.First(m => m.Id == st.MovieId);
        var cinema = MockData.Cinemas.First(c => c.Id == st.CinemaId);
        decimal total = r.SeatIds.Sum(id => MockSeatService.PriceFor(st, id[(id.LastIndexOf('-') + 1)..]))
                      + r.Combos.Sum(kv => MockData.Combos.First(c => c.Id == kv.Key).Price * kv.Value);
        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString("N"),
            Code = "CB" + Random.Shared.Next(100000, 999999),
            MovieTitle = movie.Title,
            CinemaName = cinema.Name,
            AuditoriumName = st.AuditoriumName,
            StartTime = st.StartTime,
            SeatLabels = r.SeatIds.Select(s => s[(s.LastIndexOf('-') + 1)..]).ToList(),
            TotalAmount = total,
            Status = BookingStatus.Pending
        };
        _pending[booking.Id] = (booking, r.ShowtimeId, r.SeatIds);
        return booking;
    }

    public async Task<PaymentResult> ConfirmPaymentAsync(string bookingId, PaymentMethod method, CancellationToken ct = default)
    {
        await Net.Delay(ct, 2000);
        if (!_pending.TryGetValue(bookingId, out var entry)) return new PaymentResult(false, "Đơn đặt vé không tồn tại hoặc đã hết hạn", null);
        var (b, showtimeId, seatIds) = entry;

        var paid = new Booking
        {
            Id = b.Id,
            Code = b.Code,
            MovieTitle = b.MovieTitle,
            CinemaName = b.CinemaName,
            AuditoriumName = b.AuditoriumName,
            StartTime = b.StartTime,
            SeatLabels = b.SeatLabels,
            TotalAmount = b.TotalAmount,
            Status = BookingStatus.Paid
        };
        _pending.Remove(bookingId);
        _mine.Insert(0, paid);

        if (_seats is MockSeatService mockSeats)
            mockSeats.MarkAsBooked(showtimeId, seatIds);

        return new PaymentResult(true, null, paid);
    }

    public async Task<IReadOnlyList<Booking>> GetMyTicketsAsync(CancellationToken ct = default)
    { await Net.Delay(ct, 400); return _mine; }

    public void AddPaid(Booking b) => _mine.Add(b);
}

public class MockLocationService : ILocationService
{
    public IReadOnlyList<GeoLocation> Cities => MockData.Cities;

    public async Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default)
    {
        await Net.Delay(ct, 600);
        return MockData.Cities[0];
    }
}