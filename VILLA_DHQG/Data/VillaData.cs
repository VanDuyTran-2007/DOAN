using DOAN.Models;

namespace DOAN.Data;

public record GalleryItem(string Url, string Caption);
public record Feature(string Url, string Title, string Text);
public record Service(string Glyph, string Title, string Text);

/// <summary>
/// Dữ liệu mẫu cho phần giao diện (front end), lấy cảm hứng từ Nhà khách ĐHQG-HCM.
/// Khi có cơ sở dữ liệu, chỉ cần thay lớp này bằng lớp đọc từ DB.
/// Ảnh có thể là URL (http...) hoặc ảnh trong project, ví dụ "Images/phong1.jpg"
/// (đặt Build Action của ảnh là Resource).
/// </summary>
public static class VillaData
{
    // ---------------------------------------------------------------- ẢNH
    // Ảnh thật của Nhà khách ĐHQG-HCM (nguồn: ipsc.edu.vn, httt.uit.edu.vn) — chỉ dùng cho mục đích học tập.
    private const string Ipsc = "https://ipsc.edu.vn/wp-content/uploads/";
    private const string Uit = "https://httt.uit.edu.vn/wp-content/uploads/2025/01/";

    public const string ImgDrone = Ipsc + "2025/11/DJI_20251111091405_0148_D-mp4-image.jpg";
    public const string ImgGrounds = Ipsc + "2025/11/DSC00042-scaled.jpg";
    public const string ImgBuilding = Ipsc + "2025/11/DSC00075-scaled.jpg";
    public const string ImgFeatured = Ipsc + "2025/11/anh-ok-scaled.jpg";
    public const string ImgRoom = Ipsc + "2025/11/IMG_9730-scaled.jpg";
    public const string ImgPool = Ipsc + "2025/11/DSC00032-scaled.jpg";
    public const string ImgBike = Ipsc + "2025/11/image-534.png";
    public const string ImgBus = Ipsc + "2025/12/XeBuyt1-scaled.jpg";
    public const string ImgCanteen = Ipsc + "2025/12/DSC00091-scaled.jpg";
    public const string ImgGym = Ipsc + "2025/12/DSC00006-scaled.jpg";
    public const string ImgLaundry = Ipsc + "2025/12/DSC00086-scaled.jpg";
    public const string ImgPoolIndoor = Uit + "472311792_893796319632936_171930630132811191_n-1024x768.jpg";
    public const string ImgRoomFurniture = Uit + "352358250_280252214484953_3888933479688751904_n-769x1024.jpg";
    public const string ImgRoomStudent = Uit + "454539026_793321989680370_1635796597195461278_n-950x1024.jpg";

    // Ảnh minh họa bổ sung (Unsplash) cho những góc chưa có ảnh thật: phòng tắm, villa
    private static string U(string id, int w = 1400) =>
        $"https://images.unsplash.com/photo-{id}?fm=jpg&fit=crop&w={w}&q=80";

    // ---------------------------------------------------------------- LIÊN HỆ
    public const string Hotline = "(028) 3724 4222";
    public const string Email = "ipsc@vnuhcm.edu.vn";
    public const string Address = "Đường Nguyễn Du, Khu đô thị ĐHQG-HCM, KP. Tân Hòa, P. Đông Hòa, TP. Hồ Chí Minh";

    // ---------------------------------------------------------------- TRANG CHỦ
    public static IReadOnlyList<string> HeroImages { get; } = [ImgDrone, ImgGrounds, ImgFeatured];

    public static IReadOnlyList<GalleryItem> Gallery { get; } =
    [
        new(ImgBuilding, "Tòa nhà khách ĐHQG"),
        new(ImgRoom, "Phòng lưu trú"),
        new(ImgPool, "Hồ bơi tiêu chuẩn"),
        new(ImgBike, "Xe đạp công cộng"),
        new(ImgGrounds, "Khuôn viên nhà khách"),
        new(ImgLaundry, "Dịch vụ giặt sấy"),
    ];

    public static IReadOnlyList<Feature> Features { get; } =
    [
        new(ImgCanteen, "CANTEEN – CAFÉ", "Canteen và quán cà phê ngay cạnh nhà khách, giá phù hợp sinh viên."),
        new(ImgPoolIndoor, "HỒ BƠI", "Hồ bơi có mái che, mở cửa 6:00 – 11:00 và 14:00 – 20:00."),
        new(ImgGym, "PHÒNG GYM", "Phòng tập dành cho khách lưu trú sử dụng hằng ngày."),
        new(ImgBus, "XE BUÝT NỘI KHU", "Trạm xe buýt nội khu kết nối các trường thành viên."),
    ];

    public static IReadOnlyList<Service> Services { get; } =
    [
        new("", "Wi-Fi miễn phí", "Tốc độ cao 24/24"),
        new("", "Xe buýt nội khu", "Trạm ngay cạnh nhà khách"),
        new("", "Bãi đỗ xe", "Có bảo vệ trông giữ"),
        new("", "Lễ tân 24/7", "Hỗ trợ mọi lúc"),
        new("", "Gần các trường", "0,5 – 2,5 km"),
        new("", "Lưu trú dài hạn", "Ưu đãi từ 1 tháng"),
    ];

    public static IReadOnlyList<RoomType> RoomTypes { get; } = BuildRooms();

    public static RoomType? Find(string id) => RoomTypes.FirstOrDefault(r => r.Id == id);

    // --------------------------------------------------------------------
    // Hạng phòng & giá theo thông tin công bố của Nhà khách ĐHQG-HCM (ipsc.edu.vn).
    // Diện tích và danh sách tiện nghi chi tiết là số liệu giả định cho đồ án.
    private static string[] BathroomImg => [U("1552321554-5fefe8c9ef14"), U("1584622650111-993a426fbf0a")];

    private static List<RoomType> BuildRooms()
    {
        var list = new List<RoomType>
        {
            new()
            {
                Id = "double", Name = "Phòng 1 Giường Đôi", Category = "DOUBLE ROOM", Highlight = "Giá tốt",
                ShortDescription = "Một giường đôi rộng rãi cho 2 khách – lựa chọn tiết kiệm cho cặp đôi hoặc khách công tác.",
                Description = "Phòng Double được trang bị một giường đôi, bàn học – làm việc, đèn bàn, tủ quần áo và phòng tắm riêng có nước nóng. " +
                              "Toàn bộ phòng có điều hòa và Wi-Fi tốc độ cao, phù hợp cho giảng viên, khách mời đến tham dự hội thảo hoặc phụ huynh đến thăm sinh viên.",
                Price = 400_000, Area = 20, Beds = 1, BedType = "1 giường đôi", MaxAdults = 2, MaxChildren = 0,
                View = "Hướng khuôn viên",
                Images = [ImgRoomFurniture, ImgRoom, BathroomImg[0], ImgGrounds],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bàn học & đèn bàn", "TV", "Tủ lạnh", "Nước nóng", "Tủ quần áo", "Dọn phòng hằng ngày"],
            },
            new()
            {
                Id = "twin", Name = "Phòng 2 Giường Đơn", Category = "TWIN ROOM", Highlight = "Phổ biến",
                ShortDescription = "Hai giường đơn tách biệt – tiện cho đồng nghiệp, bạn bè cùng đi công tác hoặc học tập.",
                Description = "Phòng Twin gồm hai giường đơn, bàn ghế học tập, tủ lạnh, TV và phòng tắm có bồn tắm, vòi sen, máy nước nóng. " +
                              "Đây là hạng phòng được các đoàn giảng viên, sinh viên trao đổi lựa chọn nhiều nhất nhờ mức giá hợp lý.",
                Price = 500_000, Area = 22, Beds = 2, BedType = "2 giường đơn", MaxAdults = 2, MaxChildren = 0,
                View = "Hướng khuôn viên",
                Images = [ImgRoom, ImgRoomFurniture, BathroomImg[1], ImgBuilding],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bàn ghế học tập", "TV", "Tủ lạnh", "Bồn tắm & vòi sen", "Tủ quần áo", "Dọn phòng hằng ngày"],
            },
            new()
            {
                Id = "quad", Name = "Phòng 2 Giường Đôi", Category = "QUAD ROOM",
                ShortDescription = "Hai giường đôi cho 4 khách – thoải mái cho nhóm bạn hoặc gia đình nhỏ.",
                Description = "Phòng Quad có hai giường đôi, không gian rộng hơn với khu bàn ghế sinh hoạt chung. " +
                              "Phù hợp cho nhóm sinh viên tham dự cuộc thi, hội thảo hoặc gia đình đưa con đi nhập học tại Làng Đại học.",
                Price = 500_000, Area = 28, Beds = 2, BedType = "2 giường đôi", MaxAdults = 4, MaxChildren = 0,
                View = "Hướng khuôn viên",
                Images = [ImgRoomStudent, ImgRoom, BathroomImg[0], ImgPool],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bàn ghế sinh hoạt", "TV", "Tủ lạnh", "Nước nóng", "Tủ quần áo", "Dọn phòng hằng ngày"],
            },
            new()
            {
                Id = "triple", Name = "Phòng 3 Giường Đôi", Category = "TRIPLE ROOM",
                ShortDescription = "Ba giường đôi, sức chứa đến 6 khách – lý tưởng cho đoàn học sinh, sinh viên.",
                Description = "Phòng Triple với ba giường đôi là lựa chọn kinh tế cho các đoàn đông người. " +
                              "Khách lưu trú được sử dụng hồ bơi, phòng gym, xe đạp công cộng và trạm xe buýt nội khu ngay cạnh nhà khách.",
                Price = 700_000, Area = 34, Beds = 3, BedType = "3 giường đôi", MaxAdults = 6, MaxChildren = 0,
                View = "Hướng khuôn viên",
                Images = [ImgRoom, ImgRoomStudent, BathroomImg[1], ImgBike],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bàn ghế học tập", "TV", "Tủ lạnh", "Nước nóng", "Tủ đồ cá nhân", "Dọn phòng hằng ngày"],
            },
            new()
            {
                Id = "family", Name = "Phòng Gia Đình", Category = "FAMILY ROOM",
                ShortDescription = "Hai giường đôi và một giường đơn cho 5 khách – rộng rãi cho gia đình về thăm sinh viên.",
                Description = "Phòng Gia đình gồm hai giường đôi và một giường đơn, khu sinh hoạt chung và tủ lạnh lớn. " +
                              "Phù hợp cho phụ huynh đưa con đi thi, nhập học hoặc gia đình nhiều thế hệ về thăm con em đang học tại ĐHQG-HCM.",
                Price = 600_000, Area = 32, Beds = 3, BedType = "2 giường đôi + 1 giường đơn", MaxAdults = 4, MaxChildren = 1,
                View = "Hướng khuôn viên",
                Images = [ImgRoomFurniture, ImgRoomStudent, BathroomImg[0], ImgCanteen],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Khu sinh hoạt chung", "TV", "Tủ lạnh", "Bồn tắm & vòi sen", "Tủ quần áo", "Dọn phòng hằng ngày"],
            },
            new()
            {
                Id = "villa", Name = "Villa Sân Vườn Nguyên Căn", Category = "VILLA", Highlight = "Riêng tư",
                ShortDescription = "Căn villa 3 phòng ngủ có sân vườn và bếp riêng – trọn vẹn cho đoàn chuyên gia hoặc đại gia đình.",
                Description = "Villa nguyên căn hai tầng gồm 3 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn riêng. " +
                              "Hạng phòng mở rộng của đồ án, dành cho đoàn chuyên gia lưu trú dài ngày hoặc khách mời quốc tế.",
                Price = 3_500_000, Area = 120, Beds = 4, BedType = "2 giường King + 2 giường đơn", MaxAdults = 8, MaxChildren = 2,
                View = "Sân vườn riêng",
                Images = [U("1600585154340-be6161a56a0c"), U("1580587771525-78b9dba3b914"), U("1600210492486-724fe5c67fb0"), ImgGrounds],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bếp riêng", "Sân vườn", "Máy giặt", "Phòng khách", "Smart TV", "Chỗ đỗ ô tô riêng"],
            },
        };

        // Tổng cộng 74 phòng (6 tầng) giống Nhà khách ĐHQG-HCM
        var plan = new (string Id, int Count, double Occupancy)[]
        {
            ("double", 20, 0.5),
            ("twin",   20, 0.55),
            ("quad",   14, 0.5),
            ("triple", 10, 0.45),
            ("family",  8, 0.5),
            ("villa",   2, 0.65),
        };

        var today = DateTime.Today;
        int roomNo = 0;
        foreach (var (id, count, occ) in plan)
        {
            var type = list.First(t => t.Id == id);
            for (int i = 1; i <= count; i++)
            {
                // string.GetHashCode khác nhau giữa các lần chạy -> dùng seed ổn định
                var rnd = new Random(StableSeed(id) + i * 7919);
                Room room;
                if (id == "villa")
                {
                    room = new Room { Number = $"Villa {i:00}", Floor = 0 };
                }
                else
                {
                    // Phân bổ 72 phòng lên 6 tầng, mỗi tầng 12 phòng: 101..112, 201..212, ...
                    int floor = roomNo / 12 + 1;
                    room = new Room { Number = $"P.{floor}{roomNo % 12 + 1:00}", Floor = floor };
                    roomNo++;
                }
                room.Bookings.AddRange(GenerateBookings(rnd, occ, today));
                type.Rooms.Add(room);
            }
        }

        // Cố ý cho Villa hết phòng vài ngày tới để minh họa trạng thái "Hết phòng"
        foreach (var r in list.First(t => t.Id == "villa").Rooms)
        {
            r.Bookings.RemoveAll(b => b.From < today.AddDays(4) && today.AddDays(-1) < b.To);
            r.Bookings.Add(new Booking(today.AddDays(-1), today.AddDays(4)));
        }

        return list;
    }

    private static int StableSeed(string s)
    {
        int h = 17;
        foreach (char c in s) h = h * 31 + c;
        return h;
    }

    private static List<Booking> GenerateBookings(Random rnd, double occupancy, DateTime today)
    {
        var result = new List<Booking>();
        var day = today.AddDays(-5);
        var end = today.AddDays(90);
        while (day < end)
        {
            if (rnd.NextDouble() < occupancy)
            {
                int nights = rnd.Next(1, 5);
                result.Add(new Booking(day, day.AddDays(nights)));
                day = day.AddDays(nights);
            }
            else
            {
                day = day.AddDays(rnd.Next(1, 4));
            }
        }
        return result;
    }
}
