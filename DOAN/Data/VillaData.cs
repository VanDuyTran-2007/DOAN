using DOAN.Models;

namespace DOAN.Data;

public record GalleryItem(string Url, string Caption);
public record Feature(string Url, string Title, string Text);
public record Service(string Glyph, string Title, string Text);

/// <summary>
/// Dữ liệu mẫu cho phần giao diện (front end).
/// Khi có cơ sở dữ liệu, chỉ cần thay lớp này bằng lớp đọc từ DB.
/// Ảnh đang dùng URL Unsplash; có thể thay bằng ảnh trong project, ví dụ "Images/villa1.jpg"
/// (đặt Build Action của ảnh là Resource).
/// </summary>
public static class VillaData
{
    private static string U(string id, int w = 1400) =>
        $"https://images.unsplash.com/photo-{id}?fm=jpg&fit=crop&w={w}&q=80";

    public const string Hotline = "028 3724 2160";
    public const string Email = "datphong@villadhqg.edu.vn";
    public const string Address = "Khu đô thị ĐHQG-HCM, P. Linh Trung, TP. Thủ Đức, TP. Hồ Chí Minh";

    public static IReadOnlyList<string> HeroImages { get; } =
    [
        U("1566073771259-6a8506099945", 1920),
        U("1613490493576-7fde63acd811", 1920),
        U("1470071459604-3b5ec3a7fe05", 1920),
    ];

    public static IReadOnlyList<GalleryItem> Gallery { get; } =
    [
        new(U("1571896349842-33c89424de2d"), "Hồ bơi ngoài trời"),
        new(U("1582719478250-c89cae4dc85b"), "Phòng Deluxe"),
        new(U("1414235077428-338989a2e8c0"), "Nhà hàng Hoa Sứ"),
        new(U("1441974231531-c6227db76b6e"), "Đường dạo trong rừng"),
        new(U("1600585154340-be6161a56a0c"), "Villa sân vườn"),
        new(U("1544161515-4ab6ce6db874"), "Spa & thư giãn"),
    ];

    public static IReadOnlyList<Feature> Features { get; } =
    [
        new(U("1517248135467-4c7edcad34c4", 900), "NHÀ HÀNG", "Ẩm thực Việt và buffet sáng mỗi ngày từ 6:00."),
        new(U("1520250497591-112f2f40a3f4", 900), "HỒ BƠI", "Hồ bơi ngoài trời giữa khuôn viên xanh."),
        new(U("1517502884422-41eaead166d4", 900), "HỘI THẢO", "Phòng hội nghị 30 – 200 chỗ cho sự kiện học thuật."),
        new(U("1585320806297-9794b3e4eeae", 900), "KHUÔN VIÊN", "Hơn 2 ha cây xanh, đường dạo bộ và hồ cảnh quan."),
    ];

    public static IReadOnlyList<Service> Services { get; } =
    [
        new("", "Wi-Fi miễn phí", "Tốc độ cao toàn khu"),
        new("", "Bãi đỗ xe", "Ô tô & xe máy"),
        new("", "Lễ tân 24/7", "Hỗ trợ mọi lúc"),
        new("", "Gần các trường", "Đi bộ tới ĐH thành viên"),
        new("", "Đón đoàn", "Ưu đãi cho đoàn khách"),
        new("", "Đặt linh hoạt", "Hủy miễn phí trước 24h"),
    ];

    public static IReadOnlyList<RoomType> RoomTypes { get; } = BuildRooms();

    public static RoomType? Find(string id) => RoomTypes.FirstOrDefault(r => r.Id == id);

    // --------------------------------------------------------------------
    private static List<RoomType> BuildRooms()
    {
        var list = new List<RoomType>
        {
            new()
            {
                Id = "std-single", Name = "Phòng Tiêu chuẩn Đơn", Category = "STANDARD",
                ShortDescription = "Gọn gàng, yên tĩnh – lựa chọn lý tưởng cho giảng viên, nghiên cứu sinh đi công tác.",
                Description = "Phòng Tiêu chuẩn Đơn được thiết kế tối giản với tông gỗ ấm, bàn làm việc rộng và cửa sổ nhìn ra vườn cây. " +
                              "Không gian nhỏ gọn nhưng đầy đủ tiện nghi, phù hợp cho khách lưu trú ngắn ngày hoặc đến tham dự hội thảo tại ĐHQG.",
                Price = 450_000, Area = 20, Beds = 1, BedType = "Giường đơn 1m2", MaxAdults = 1, MaxChildren = 0,
                View = "Hướng vườn",
                Images = [U("1631049307264-da0ec9d70304"), U("1595576508898-0ad5c879a061"), U("1584622650111-993a426fbf0a"), U("1505693416388-ac5ce068fe85")],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bàn làm việc", "TV 32\"", "Nước nóng", "Tủ quần áo", "Ấm đun nước", "Dép & khăn tắm"],
            },
            new()
            {
                Id = "std-twin", Name = "Phòng Tiêu chuẩn Đôi", Category = "STANDARD", Highlight = "Phổ biến",
                ShortDescription = "Hai giường đơn tiện lợi cho đồng nghiệp hoặc bạn bè cùng đi công tác, học tập.",
                Description = "Phòng có hai giường đơn tách biệt, bàn làm việc chung và khu vực tủ đồ rộng. " +
                              "Đây là hạng phòng được các đoàn giảng viên, sinh viên trao đổi lựa chọn nhiều nhất nhờ mức giá hợp lý.",
                Price = 550_000, Area = 26, Beds = 2, BedType = "2 giường đơn 1m2", MaxAdults = 2, MaxChildren = 0,
                View = "Hướng vườn",
                Images = [U("1590490360182-c33d57733427"), U("1596394516093-501ba68a0ba6"), U("1552321554-5fefe8c9ef14"), U("1522771739844-6a9f6d5f14af")],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bàn làm việc", "TV 40\"", "Tủ lạnh mini", "Nước nóng", "Ấm đun nước", "Két an toàn"],
            },
            new()
            {
                Id = "deluxe", Name = "Phòng Deluxe Hướng Hồ", Category = "DELUXE",
                ShortDescription = "Giường đôi lớn, ban công riêng nhìn ra hồ cảnh quan – yên bình như một kỳ nghỉ.",
                Description = "Phòng Deluxe sở hữu ban công riêng hướng thẳng ra hồ cảnh quan trong khuôn viên. " +
                              "Nội thất gỗ sồi, chăn ga cotton cao cấp, bồn tắm đứng kính và góc thư giãn cạnh cửa sổ lớn mang lại cảm giác như đang ở một resort.",
                Price = 750_000, Area = 32, Beds = 1, BedType = "Giường đôi 1m8", MaxAdults = 2, MaxChildren = 1,
                View = "Hướng hồ, có ban công",
                Images = [U("1582719478250-c89cae4dc85b"), U("1611892440504-42a792e24d32"), U("1584132967334-10e028bd69f7"), U("1552321554-5fefe8c9ef14")],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Ban công riêng", "Smart TV 43\"", "Tủ lạnh mini", "Máy sấy tóc", "Két an toàn", "Ấm đun nước", "Bữa sáng"],
            },
            new()
            {
                Id = "family", Name = "Phòng Gia đình", Category = "FAMILY",
                ShortDescription = "Hai giường đôi và góc sinh hoạt chung – rộng rãi cho gia đình có con nhỏ.",
                Description = "Phòng Gia đình rộng 44 m² với hai giường đôi, sofa và bàn ăn nhỏ. " +
                              "Phù hợp cho phụ huynh đưa con đi thi, nhập học hoặc gia đình về thăm sinh viên. Có thể kê thêm nôi em bé theo yêu cầu.",
                Price = 1_150_000, Area = 44, Beds = 2, BedType = "2 giường đôi 1m6", MaxAdults = 4, MaxChildren = 2,
                View = "Hướng vườn",
                Images = [U("1618773928121-c32242e63f39"), U("1560448204-e02f11c3d0e2"), U("1540518614846-7eded433c457"), U("1584622650111-993a426fbf0a")],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Sofa", "Smart TV 50\"", "Tủ lạnh", "Bàn ăn nhỏ", "Nôi em bé (theo yêu cầu)", "Máy sấy tóc", "Bữa sáng"],
            },
            new()
            {
                Id = "suite", Name = "Junior Suite Rừng Thông", Category = "SUITE", Highlight = "Cao cấp",
                ShortDescription = "Phòng ngủ và phòng khách riêng biệt, cửa kính lớn ôm trọn hàng cây xanh.",
                Description = "Junior Suite nằm ở tầng cao nhất với phòng khách riêng, bàn làm việc gỗ nguyên khối và bồn tắm nằm. " +
                              "Hạng phòng dành cho chuyên gia, khách mời quốc tế và những ai muốn một không gian riêng tư, sang trọng.",
                Price = 1_450_000, Area = 50, Beds = 1, BedType = "Giường King 2m", MaxAdults = 2, MaxChildren = 1,
                View = "Hướng rừng cây, tầng cao",
                Images = [U("1616594039964-ae9021a400a0"), U("1600210492486-724fe5c67fb0"), U("1560185007-cde436f6a4d0"), U("1552321554-5fefe8c9ef14")],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Phòng khách riêng", "Bồn tắm nằm", "Smart TV 55\"", "Máy pha cà phê", "Minibar", "Áo choàng tắm", "Bữa sáng"],
            },
            new()
            {
                Id = "villa", Name = "Villa Sân Vườn Nguyên Căn", Category = "VILLA", Highlight = "Riêng tư",
                ShortDescription = "Căn villa 3 phòng ngủ có sân vườn và bếp riêng – trọn vẹn cho nhóm hoặc đại gia đình.",
                Description = "Villa nguyên căn hai tầng gồm 3 phòng ngủ, phòng khách, bếp đầy đủ dụng cụ và sân vườn BBQ riêng. " +
                              "Thích hợp cho đoàn chuyên gia lưu trú dài ngày, gia đình nhiều thế hệ hoặc nhóm bạn tổ chức tiệc nhỏ.",
                Price = 3_900_000, Area = 120, Beds = 4, BedType = "2 giường King + 2 giường đơn", MaxAdults = 8, MaxChildren = 2,
                View = "Sân vườn riêng",
                Images = [U("1600585154340-be6161a56a0c"), U("1580587771525-78b9dba3b914"), U("1600210492486-724fe5c67fb0"), U("1564013799919-ab600027ffc6")],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Bếp riêng", "Sân vườn BBQ", "Máy giặt", "Phòng khách", "Smart TV 65\"", "Chỗ đỗ ô tô riêng", "Bữa sáng"],
            },
            new()
            {
                Id = "group", Name = "Phòng Đoàn Sinh viên", Category = "GROUP",
                ShortDescription = "Giường tầng tiện lợi cho đoàn sinh viên tham dự hội thảo, cuộc thi, giao lưu.",
                Description = "Phòng tập thể với 4 giường tầng (8 chỗ), tủ khóa cá nhân cho từng khách và phòng tắm rộng. " +
                              "Giá ưu đãi dành cho đoàn sinh viên, học sinh đến tham dự các sự kiện tại Làng Đại học.",
                Price = 1_200_000, Area = 40, Beds = 4, BedType = "4 giường tầng", MaxAdults = 8, MaxChildren = 0,
                View = "Hướng sân trong",
                Images = [U("1596394516093-501ba68a0ba6"), U("1540518614846-7eded433c457"), U("1584622650111-993a426fbf0a"), U("1562774053-701939374585")],
                Amenities = ["Wi-Fi miễn phí", "Điều hòa", "Tủ khóa cá nhân", "Nước nóng", "Bàn học chung", "Quạt trần"],
            },
        };

        // Tạo danh sách phòng + lịch đặt mẫu (cố định nhờ seed)
        var plan = new (string Id, string Prefix, int Count, double Occupancy)[]
        {
            ("std-single", "1", 8, 0.45),
            ("std-twin",   "2", 10, 0.55),
            ("deluxe",     "3", 6, 0.6),
            ("family",     "4", 4, 0.5),
            ("suite",      "5", 3, 0.5),
            ("villa",      "V", 2, 0.65),
            ("group",      "6", 3, 0.4),
        };

        var today = DateTime.Today;
        foreach (var (id, prefix, count, occ) in plan)
        {
            var type = list.First(t => t.Id == id);
            for (int i = 1; i <= count; i++)
            {
                // string.GetHashCode khác nhau giữa các lần chạy -> dùng seed ổn định
                var rnd = new Random(StableSeed(id) + i * 7919);

                var room = new Room
                {
                    Number = prefix == "V" ? $"Villa {i:00}" : $"P.{prefix}{i:00}",
                    Floor = prefix == "V" ? 1 : int.Parse(prefix),
                    Bookings = GenerateBookings(rnd, occ, today),
                };
                type.Rooms.Add(room);
            }
        }

        // Cố ý cho Junior Suite hết phòng vài ngày tới để minh họa trạng thái "Hết phòng"
        foreach (var r in list.First(t => t.Id == "suite").Rooms)
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
