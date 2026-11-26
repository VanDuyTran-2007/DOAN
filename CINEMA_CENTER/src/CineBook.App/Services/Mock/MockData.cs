using CineBook.Models;

namespace CineBook.Services.Mock;

/// <summary>
/// Dữ liệu giả dùng chung cho các Mock service. Toàn bộ tên phim là TÊN HƯ CẤU (tránh bản quyền).
/// Backend team: file này sẽ bị bỏ khi thay bằng service thật.
/// </summary>
public static class MockData
{
    public static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

    public static readonly IReadOnlyList<Brand> Brands =
    [
        new Brand { Id = "cgv", Name = "CGV", ThemeColorHex = "#C8102E" },
        new Brand { Id = "lotte", Name = "Lotte Cinema", ThemeColorHex = "#E4002B" },
        new Brand { Id = "bhd", Name = "BHD Star", ThemeColorHex = "#2D6CDF" },
        new Brand { Id = "cinestar", Name = "Cinestar", ThemeColorHex = "#7B3FE4" },
    ];

    private static Cinema C(string id, string brand, string name, string addr, string district, string city, double lat, double lng) =>
        new() { Id = id, BrandId = brand, Name = name, Address = addr, District = district, City = city, Latitude = lat, Longitude = lng,
                Brand = Brands.First(b => b.Id == brand) };

    public static readonly IReadOnlyList<Cinema> Cinemas =
    [
        C("cgv-1", "cgv", "CGV Vincom Đồng Khởi", "72 Lê Thánh Tôn, Bến Nghé", "Quận 1", "TP.HCM", 10.7780, 106.7026),
        C("cgv-2", "cgv", "CGV Landmark 81", "720A Điện Biên Phủ, Bình Thạnh", "Bình Thạnh", "TP.HCM", 10.7952, 106.7218),
        C("cgv-3", "cgv", "CGV Vincom Bà Triệu", "191 Bà Triệu, Hai Bà Trưng", "Hai Bà Trưng", "Hà Nội", 21.0125, 105.8497),
        C("cgv-4", "cgv", "CGV Aeon Bình Dương", "1 Đại lộ Bình Dương, Thuận An", "Thuận An", "Bình Dương", 10.9167, 106.7190),
        C("lotte-1", "lotte", "Lotte Cinema Cantavil", "Cantavil, Xa Lộ Hà Nội, Thủ Đức", "TP. Thủ Đức", "TP.HCM", 10.8006, 106.7421),
        C("lotte-2", "lotte", "Lotte Cinema Nam Sài Gòn", "469 Nguyễn Hữu Thọ, Tân Hưng", "Quận 7", "TP.HCM", 10.7421, 106.7014),
        C("lotte-3", "lotte", "Lotte Cinema Hà Đông", "Mê Linh Plaza, Hà Đông", "Hà Đông", "Hà Nội", 20.9712, 105.7790),
        C("lotte-4", "lotte", "Lotte Cinema Bình Dương", "Lotte Mart, Thủ Dầu Một", "Thủ Dầu Một", "Bình Dương", 10.9818, 106.6500),
        C("bhd-1", "bhd", "BHD Star Vincom Quang Trung", "190 Quang Trung, Gò Vấp", "Gò Vấp", "TP.HCM", 10.8264, 106.6817),
        C("bhd-2", "bhd", "BHD Star Phạm Ngọc Thạch", "Vincom Plaza, Phạm Ngọc Thạch", "Đống Đa", "Hà Nội", 21.0067, 105.8309),
        C("bhd-3", "bhd", "BHD Star Bitexco", "2 Hải Triều, Bến Nghé", "Quận 1", "TP.HCM", 10.7716, 106.7044),
        C("cs-1", "cinestar", "Cinestar Quốc Thanh", "271 Nguyễn Trãi, Quận 1", "Quận 1", "TP.HCM", 10.7650, 106.6890),
        C("cs-2", "cinestar", "Cinestar Hai Bà Trưng", "135 Hai Bà Trưng, Quận 3", "Quận 3", "TP.HCM", 10.7886, 106.6990),
        C("cs-3", "cinestar", "Cinestar Mỹ Phước", "Khu đô thị Mỹ Phước, Bến Cát", "Bến Cát", "Bình Dương", 11.1130, 106.6020),
        C("cs-4", "cinestar", "Cinestar Hà Nội Royal City", "72A Nguyễn Trãi, Thanh Xuân", "Thanh Xuân", "Hà Nội", 21.0025, 105.8150),
    ];

    private static Movie M(string id, string title, string orig, string[] genres, int min, AgeRating age, int dayOffset,
                           double rating, MovieStatus st, bool hot, string color, string director, string[] cast, string desc) =>
        new() { Id = id, Title = title, OriginalTitle = orig, Genres = genres, DurationMinutes = min, AgeRating = age,
                ReleaseDate = DateTime.Today.AddDays(dayOffset), Rating = rating, Status = st, IsHot = hot,
                PlaceholderColorHex = color, Director = director, Cast = cast, Description = desc,
                TrailerUrl = "https://example.com/trailer/" + id };

    public static readonly IReadOnlyList<Movie> Movies =
    [
        M("m01", "Bóng Đêm Thành Phố", "City of Shadows", ["Hành động", "Hình sự"], 128, AgeRating.T16, -20, 8.1, MovieStatus.NowShowing, true, "#7A1030", "Trần Minh Khải", ["Lâm Vỹ", "Ngọc Hân", "Quốc Bảo"], "Một cựu cảnh sát buộc phải quay lại đường phố Sài Gòn về đêm để truy tìm kẻ đứng sau vụ cướp ngân hàng bí ẩn."),
        M("m02", "Lời Nguyền Căn Nhà Cũ", "The Old House", ["Kinh dị"], 105, AgeRating.T18, -12, 7.4, MovieStatus.NowShowing, true, "#3A0F1A", "Phạm Thu Hà", ["Thanh Mai", "Đức Anh"], "Gia đình trẻ chuyển vào căn nhà cổ và dần phát hiện những bí mật đáng sợ ẩn dưới tầng hầm."),
        M("m03", "Phi Đội Mặt Trăng", "Moon Squadron", ["Hoạt hình", "Phiêu lưu"], 98, AgeRating.P, -8, 8.6, MovieStatus.NowShowing, true, "#1D5FA8", "Lê Anh Tuấn", ["Bé Na (lồng tiếng)", "Hoàng Long (lồng tiếng)"], "Nhóm bạn nhỏ cùng chú robot lên mặt trăng để cứu lấy thành phố khỏi một cơn bão sao."),
        M("m04", "Mùa Hè Của Chúng Ta", "Our Summer", ["Tình cảm", "Hài"], 112, AgeRating.T13, -15, 7.8, MovieStatus.NowShowing, false, "#C2527A", "Nguyễn Thảo Vy", ["Khánh Linh", "Gia Huy"], "Hai người bạn thân thời cấp ba gặp lại nhau sau mười năm trong một chuyến đi xuyên Việt."),
        M("m05", "Đế Chế Rồng Lửa", "Dragonfire Empire", ["Hành động", "Giả tưởng"], 145, AgeRating.T13, -5, 8.9, MovieStatus.NowShowing, true, "#B5471B", "Đỗ Quang Vinh", ["Hải Đăng", "Bích Phương", "Minh Tâm"], "Cuộc chiến giành ngai vàng giữa các bộ tộc rồng bùng nổ khi lời tiên tri cổ xưa ứng nghiệm."),
        M("m06", "Hồ Sơ Mất Tích", "Missing File", ["Hình sự", "Kinh dị"], 119, AgeRating.T16, -25, 7.2, MovieStatus.NowShowing, false, "#24476B", "Vũ Hoàng Nam", ["Thu Trang", "Tiến Luật"], "Một nữ phóng viên lần theo manh mối của chuỗi vụ mất tích tại một thị trấn ven biển."),
        M("m07", "Chú Mèo Đi Tìm Biển", "The Cat Who Sought the Sea", ["Hoạt hình", "Gia đình"], 92, AgeRating.K, -30, 8.3, MovieStatus.NowShowing, false, "#2A8F8A", "Hoàng Mỹ Linh", ["Lồng tiếng: Ngọc Lan"], "Chú mèo nhỏ rời làng chài để tìm hiểu vì sao biển lại biết hát."),
        M("m08", "Cuộc Đua Cuối Cùng", "Last Lap", ["Hành động", "Thể thao"], 124, AgeRating.T13, -3, 7.6, MovieStatus.NowShowing, false, "#555F7A", "Bùi Thế Sơn", ["Tuấn Kiệt", "Mỹ Duyên"], "Tay đua kỳ cựu quyết định ra đường đua một lần cuối để cứu đội xe của gia đình."),
        M("m09", "Vùng Đất Quên Lãng", "Forgotten Land", ["Giả tưởng", "Phiêu lưu"], 135, AgeRating.T13, 10, 0, MovieStatus.ComingSoon, true, "#4B2C83", "Trần Minh Khải", ["Hà Anh", "Văn Toàn"], "Một cánh cổng cổ mở ra vùng đất nơi mọi ký ức bị lãng quên đều sống dậy."),
        M("m10", "Đám Cưới Bất Đắc Dĩ", "Reluctant Wedding", ["Hài", "Tình cảm"], 104, AgeRating.T13, 17, 0, MovieStatus.ComingSoon, false, "#C98A3C", "Nguyễn Thảo Vy", ["Trúc Anh", "Khôi Nguyên"], "Hai kẻ thù không đội trời chung bị gia đình ép làm đám cưới giả để cứu công ty."),
        M("m11", "Tiếng Gọi Từ Vực Sâu", "Call from the Deep", ["Kinh dị", "Hồi hộp"], 111, AgeRating.T18, 24, 0, MovieStatus.ComingSoon, true, "#10303F", "Phạm Thu Hà", ["Lan Phương", "Quang Huy"], "Đội thợ lặn nhận được tín hiệu cầu cứu từ một con tàu đã chìm hàng chục năm."),
        M("m12", "Siêu Nhân Nhí", "Little Heroes", ["Hoạt hình", "Hài"], 90, AgeRating.P, 31, 0, MovieStatus.ComingSoon, false, "#D1563A", "Lê Anh Tuấn", ["Lồng tiếng: Bảo Nam"], "Biệt đội siêu nhân tí hon bảo vệ khu phố trước kế hoạch của giáo sư Bụi."),
    ];

    public static readonly IReadOnlyList<GeoLocation> Cities =
    [
        new(10.7769, 106.7009, "TP.HCM"),
        new(21.0285, 105.8542, "Hà Nội"),
        new(10.9804, 106.6519, "Bình Dương"),
    ];

    public static readonly IReadOnlyList<Combo> Combos =
    [
        new() { Id = "cb1", Name = "Combo Solo", Description = "1 bắp (M) + 1 nước (M)", Price = 79000 },
        new() { Id = "cb2", Name = "Combo Couple", Description = "1 bắp (L) + 2 nước (M)", Price = 109000 },
        new() { Id = "cb3", Name = "Combo Family", Description = "2 bắp (L) + 4 nước (M)", Price = 199000 },
        new() { Id = "cb4", Name = "Nước ngọt", Description = "Nước ngọt size L", Price = 39000 },
        new() { Id = "cb5", Name = "Bắp caramel", Description = "Bắp vị caramel size L", Price = 69000 },
    ];

    public static readonly IReadOnlyList<string> Genres =
        ["Hành động", "Kinh dị", "Hoạt hình", "Tình cảm", "Hài", "Hình sự", "Giả tưởng", "Phiêu lưu", "Gia đình", "Thể thao", "Hồi hộp"];

    /// <summary>Hash ổn định giữa các lần chạy (string.GetHashCode của .NET bị random hoá).</summary>
    public static int StableHash(string s)
    {
        unchecked { int h = 17; foreach (var c in s) h = h * 31 + c; return h; }
    }

    // Id suất chiếu có dạng: "st|movieId|cinemaId|yyyyMMdd|slot"
    // NOTE-DB: backend thật dùng Id riêng (guid/bigint); chuỗi ghép này chỉ để mock dựng lại suất chiếu từ Id.
    private static readonly (int h, int m)[] Slots = [(9, 30), (11, 45), (14, 0), (16, 30), (19, 0), (21, 15), (22, 40)];

    public static Showtime BuildShowtime(string movieId, string cinemaId, DateTime date, int slot)
    {
        var rnd = new Random(StableHash($"{movieId}{cinemaId}{slot}"));
        var format = (MovieFormat)rnd.Next(0, 4);
        var lang = rnd.Next(0, 3) == 0 ? SubtitleLanguage.Dubbed : SubtitleLanguage.Subtitled;
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, Slots[slot].h, Slots[slot].m, 0, VnOffset);
        decimal basePrice = format switch { MovieFormat.IMAX => 150000, MovieFormat.FourDX => 170000, MovieFormat.ThreeD => 120000, _ => 85000 };
        if (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) basePrice += 10000;
        return new Showtime
        {
            Id = $"st|{movieId}|{cinemaId}|{date:yyyyMMdd}|{slot}",
            MovieId = movieId, CinemaId = cinemaId, AuditoriumId = $"{cinemaId}-r{slot % 3 + 1}",
            AuditoriumName = $"Phòng {slot % 3 + 1}", StartTime = start, Format = format, Language = lang, BasePrice = basePrice
        };
    }

    public static Showtime? ParseShowtime(string id)
    {
        var p = id.Split('|');
        if (p.Length != 5 || !DateTime.TryParseExact(p[3], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var d)) return null;
        return BuildShowtime(p[1], p[2], d, int.Parse(p[4]));
    }

    public static IEnumerable<Showtime> ShowtimesFor(string movieId, string cinemaId, DateTime date)
    {
        // Mỗi cặp phim-rạp có 3-5 suất/ngày, chọn theo seed để ổn định.
        var rnd = new Random(StableHash(movieId + cinemaId + date.ToString("yyyyMMdd")));
        int count = rnd.Next(3, 6);
        return Enumerable.Range(0, Slots.Length).OrderBy(_ => rnd.Next()).Take(count).OrderBy(s => s)
                         .Select(s => BuildShowtime(movieId, cinemaId, date, s));
    }

    /// <summary>Phim đang chiếu có được chiếu ở rạp này hay không (mỗi rạp chiếu ~70% phim).</summary>
    public static bool IsScreening(string movieId, string cinemaId) => Math.Abs(StableHash(movieId + cinemaId) % 10) < 7;
}
