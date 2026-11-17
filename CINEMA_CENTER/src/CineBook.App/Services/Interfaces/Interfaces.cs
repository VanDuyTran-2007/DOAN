using CineBook.Models;

namespace CineBook.Services.Interfaces;

// Hợp đồng giữa frontend và backend. Backend team chỉ cần implement các interface này (xem docs/BACKEND_NOTES.md).

public interface IMovieService
{
    // TODO-BACKEND: GET /api/movies?status=NowShowing → List<Movie>. Lỗi: 500 (server), 408 (timeout).
    Task<IReadOnlyList<Movie>> GetNowShowingAsync(CancellationToken ct = default);
    // TODO-BACKEND: GET /api/movies?status=ComingSoon → List<Movie>
    Task<IReadOnlyList<Movie>> GetComingSoonAsync(CancellationToken ct = default);
    // TODO-BACKEND: GET /api/movies/hot → List<Movie>
    Task<IReadOnlyList<Movie>> GetHotAsync(CancellationToken ct = default);
    // TODO-BACKEND: GET /api/movies/{id} → Movie. Lỗi: 404 nếu không có.
    Task<Movie?> GetByIdAsync(string id, CancellationToken ct = default);
    // TODO-BACKEND: GET /api/movies/search?q= → List<Movie>. NOTE-DB: cần full-text index trên Title/OriginalTitle.
    Task<IReadOnlyList<Movie>> SearchAsync(string keyword, CancellationToken ct = default);
    // TODO-BACKEND: GET /api/movies?genre=&status=&format=&sort=&page=&pageSize= → PagedResult<Movie>
    Task<PagedResult<Movie>> GetByGenreAsync(MovieQuery query, CancellationToken ct = default);
    // TODO-BACKEND: GET /api/genres → List<string>
    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct = default);
}

public interface ICinemaService
{
    // TODO-BACKEND: GET /api/brands → List<Brand>
    Task<IReadOnlyList<Brand>> GetBrandsAsync(CancellationToken ct = default);
    // TODO-BACKEND: GET /api/cinemas?brandId=&city=&district= → List<Cinema>
    Task<IReadOnlyList<Cinema>> GetCinemasAsync(CinemaFilter filter, CancellationToken ct = default);
    // TODO-BACKEND: GET /api/cinemas/nearby?lat=&lng=&radiusKm= → List<Cinema> (đã có DistanceKm, sắp xếp tăng dần)
    // NOTE-DB: dùng Haversine hoặc kiểu geography + index không gian.
    Task<IReadOnlyList<Cinema>> GetNearbyAsync(double lat, double lng, double radiusKm, CancellationToken ct = default);
    // TODO-BACKEND: GET /api/cinemas/{id}/movies → List<Movie> (phim đang chiếu tại rạp)
    Task<IReadOnlyList<Movie>> GetMoviesAtCinemaAsync(string cinemaId, CancellationToken ct = default);
}

public interface IShowtimeService
{
    // TODO-BACKEND: GET /api/showtimes?movieId=&cinemaId=&date= → List<Showtime>
    // NOTE-DB: date là ngày theo giờ Việt Nam (UTC+7); server đổi sang khoảng UTC khi truy vấn.
    Task<IReadOnlyList<Showtime>> GetShowtimesAsync(string movieId, string? cinemaId, DateTime date, CancellationToken ct = default);
    // TODO-BACKEND: GET /api/showtimes/{id} → Showtime
    Task<Showtime?> GetByIdAsync(string showtimeId, CancellationToken ct = default);
}

public interface ISeatService
{
    // TODO-BACKEND: GET /api/showtimes/{id}/seats → SeatMap
    // NOTE-DB: trạng thái ghế = join Seats với BookingSeats và SeatHolds theo showtime.
    Task<SeatMap> GetSeatMapAsync(string showtimeId, CancellationToken ct = default);

    // TODO-BACKEND: POST /api/showtimes/{id}/holds  body {seatIds[]} → SeatHold {holdId, expiresAt}. Lỗi: 409 nếu ghế đã bị giữ/đặt, 422 nếu vi phạm rule ghế lẻ hoặc quá 8 ghế.
    // WARNING-BACKEND: race condition – 2 người cùng chọn 1 ghế. Dùng transaction + unique constraint (ShowtimeId, SeatId) hoặc optimistic concurrency.
    // WARNING-BACKEND: TTL giữ ghế 10 phút, job dọn hold hết hạn. Gợi ý SignalR đẩy trạng thái ghế realtime.
    // WARNING-BACKEND: validate lại tối đa 8 ghế/lần và rule không để 1 ghế trống lẻ.
    Task<SeatHold> HoldSeatsAsync(string showtimeId, IReadOnlyList<string> seatIds, CancellationToken ct = default);

    // TODO-BACKEND: DELETE /api/holds/{holdId} → 204
    Task ReleaseSeatsAsync(string holdId, CancellationToken ct = default);
}

public interface IAuthService
{
    User? CurrentUser { get; }
    event EventHandler? AuthStateChanged;
    // TODO-BACKEND: POST /api/auth/sign-in body {identity, password} → {accessToken, refreshToken, user}. Lỗi: 401 sai thông tin, 423 khoá tài khoản.
    // WARNING-BACKEND: mật khẩu chỉ hash ở server (Argon2/BCrypt). Client lưu token bằng DPAPI (ProtectedData); dùng refresh token.
    Task<AuthResult> SignInAsync(string identity, string password, CancellationToken ct = default);
    // TODO-BACKEND: POST /api/auth/sign-up body SignUpRequest → AuthResult. Lỗi: 409 email/SĐT đã tồn tại.
    // NOTE-DB: Users unique (Email), unique (Phone).
    Task<AuthResult> SignUpAsync(SignUpRequest request, CancellationToken ct = default);
    // TODO-BACKEND: POST /api/auth/sign-out (thu hồi refresh token)
    Task SignOutAsync(CancellationToken ct = default);
}

public interface IBookingService
{
    // TODO-BACKEND: GET /api/combos → List<Combo>
    Task<IReadOnlyList<Combo>> GetCombosAsync(CancellationToken ct = default);

    // TODO-BACKEND: POST /api/bookings body CreateBookingRequest → Booking (Status=Pending). Lỗi: 409 hold hết hạn, 422 dữ liệu sai.
    // WARNING-BACKEND: giá vé phải tính lại ở server (loại ghế, định dạng phim, ngày trong tuần/lễ, hạng thành viên); không lấy giá client gửi lên.
    // WARNING-BACKEND: dùng Idempotency-Key để tránh tạo trùng đơn khi bấm nhiều lần.
    Task<Booking> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default);

    // TODO-BACKEND: POST /api/bookings/{id}/pay body {method} → PaymentResult. Thực tế: trả URL cổng thanh toán và chờ callback/IPN.
    // WARNING-BACKEND: trạng thái đơn chỉ đổi theo IPN đã xác thực chữ ký (Pending/Paid/Failed/Expired/Refunded), không tin client.
    Task<PaymentResult> ConfirmPaymentAsync(string bookingId, PaymentMethod method, CancellationToken ct = default);

    // TODO-BACKEND: GET /api/me/bookings → List<Booking> (cần token)
    Task<IReadOnlyList<Booking>> GetMyTicketsAsync(CancellationToken ct = default);
}

public interface ILocationService
{
    // TODO-BACKEND: (tuỳ chọn) GET /api/geo/resolve?ip= nếu muốn định vị theo IP. Hiện lấy qua GPS/Windows Location trên client.
    // Trả null nếu người dùng từ chối quyền vị trí → UI cho chọn thành phố thủ công.
    Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default);
    IReadOnlyList<GeoLocation> Cities { get; }
}
