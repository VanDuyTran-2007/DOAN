# CineBook – CINEMA_CENTER

App đặt vé xem phim đa hệ thống rạp (CGV, Lotte Cinema, BHD Star, Cinestar). Solution gồm ứng dụng WPF và API ASP.NET Core dùng SQL Server. Đăng nhập, đăng ký và tài khoản cá nhân trong WPF gọi API. Danh mục và luồng đặt vé WPF hiện dùng dữ liệu demo. Xem [ghi chú backend](docs/BACKEND_NOTES.md).

## Cách chạy

Yêu cầu: Visual Studio 2022/2026 (workload *.NET desktop development*) hoặc .NET SDK 10, Windows 10/11.

```powershell
dotnet run --project src/CineBook.App
```

Hoặc mở `CINEMA_CENTER/CINEMA_CENTER.slnx` trong Visual Studio và chạy project `CineBook.App`.

Để đăng ký hoặc đăng nhập, khởi tạo database và chạy API theo hướng dẫn bên dưới. Sau khi đăng nhập, mở menu tài khoản ở góc trên bên phải và chọn **Tài khoản**.

### Khởi tạo database cho API

Từ thư mục `CINEMA_CENTER`, chạy các lệnh sau trong PowerShell. Mặc định API dùng SQL Server tại `localhost` và database `CineBookDB`. Nếu dùng SQL Server Express LocalDB, đặt chuỗi kết nối cho phiên PowerShell hiện tại trước khi chạy:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=CineBookDB;Trusted_Connection=True;TrustServerCertificate=True;'
```

```powershell
dotnet run --project src/CineBook.Api --no-launch-profile -- --init-db
dotnet run --project src/CineBook.Api
```

`--init-db` áp dụng migration và nạp dữ liệu demo từ `MockData.cs`. Dùng một database mới hoặc đã được quản lý bằng migration của CineBook. Chạy lệnh một lần sau khi cập nhật lên phiên bản có trang tài khoản. Nếu dữ liệu cũ có email chính hoặc số điện thoại trùng, xử lý các tài khoản trùng trước khi áp dụng migration `AccountProfiles`.

Sau khi khởi tạo, API tự bổ sung một đợt lịch 7 ngày theo giờ Việt Nam khi hết đợt demo hiện tại. API kiểm tra khi khởi động và định kỳ trong lúc chạy. Các suất chiếu cũ vẫn được giữ để tra cứu vé.

WPF mặc định gọi `https://localhost:7085/`. Nếu môi trường phát triển chưa tin cậy chứng chỉ HTTPS của .NET, chạy `dotnet dev-certs https --trust`. Nếu API chạy ở địa chỉ khác, đặt biến môi trường `CineBook__ApiBaseUrl` khi khởi động WPF. Có thể kiểm tra kết nối tại `https://localhost:7085/api/movies`.

**Tổng chi tiêu** lấy từ các đơn đã thanh toán của tài khoản trong SQL Server. Đơn mock trên WPF chưa được đồng bộ vào số này. Tab **Voucher** chỉ hiển thị bản ghi trong bảng `Vouchers` đang ở khoảng hiệu lực UTC.

Nếu muốn nhiều máy dùng cùng dữ liệu, xem [triển khai API chung và xem dữ liệu trong SSMS](docs/SHARED_API.md).

## Cấu trúc

```
CINEMA_CENTER/
├─ CINEMA_CENTER.slnx
├─ docs/
│  ├─ BACKEND_NOTES.md        ← tài liệu cho backend/database
│  └─ screenshots/            ← ảnh chụp các màn hình
└─ src/CineBook.App/
   ├─ App.xaml(.cs)           ← cấu hình Host + DI (nơi đổi Mock → Api)
   ├─ MainWindow.xaml         ← title bar tự vẽ + Header + vùng điều hướng + Toast
   ├─ Themes/                 ← Colors, Typography, Controls, Icons, FormStyles
   ├─ Models/                 ← DTO + enum
   ├─ Services/
   │  ├─ Interfaces/          ← hợp đồng frontend ↔ backend
   │  └─ Mock/                ← dữ liệu giả + Task.Delay giả độ trễ mạng
   ├─ Navigation/             ← INavigationService (+ Back)
   ├─ ViewModels/             ← MVVM (CommunityToolkit.Mvvm)
   ├─ Views/                  ← Home, MovieDetail, Cinemas, Categories, Auth, Booking, MyTickets
   ├─ Controls/               ← UserControl tái sử dụng (MoviePosterCard, SeatMapControl, HeaderView, ...)
   ├─ Converters/  Helpers/
```

## Màn hình

| Màn hình | Ghi chú |
|---|---|
| Trang chủ | Banner lấy poster làm nền, kéo chuột ngang hoặc tự chuyển sau 6 giây, rạp gần bạn theo thành phố đã chọn |
| Chi tiết phim | Thanh chọn 7 ngày → lọc hãng/thành phố → rạp + giờ chiếu theo định dạng |
| Rạp chiếu | Lọc hãng/thành phố/quận; chọn rạp xem phim + lịch chiếu |
| Danh mục / Phim | Lọc thể loại, trạng thái, định dạng, sắp xếp; "Tải thêm" |
| Đăng nhập / Đăng ký | Xác thực qua API, định dạng ngày sinh tự động và chỉ bật nút gửi khi form hợp lệ |
| Tài khoản cá nhân | Hồ sơ lưu vào SQL Server, ảnh đại diện, xác thực và đổi mật khẩu, voucher đang phát hành |
| Đặt vé | ① Chọn suất → ② Chọn ghế (sơ đồ sinh từ dữ liệu, zoom, đếm ngược 10:00) → ③ Bắp nước → ④ Thanh toán (mock 2s) → ⑤ Vé + QR giả |
| Vé của tôi | Danh sách vé đã thanh toán trong phiên |

## Quy ước code

- **MVVM**: code-behind chỉ chứa việc thuần UI (animation, zoom, căn vùng kính mờ, `PasswordHelper`).
- **Không hard-code màu** trong View: dùng `{StaticResource ...}` từ `Themes/Colors.xaml`. (Màu động theo dữ liệu – brand, poster – đi qua converter.)
- Comment tiếng Việt; tên class/biến tiếng Anh.
- 3 tag ghi chú cho backend (tìm bằng Task List của Visual Studio):

| Tag | Dùng khi |
|---|---|
| `// TODO-BACKEND:` | Chỗ cần API thật (endpoint, input, output, lỗi) |
| `// NOTE-DB:` | Gợi ý bảng/cột/quan hệ/index |
| `// WARNING-BACKEND:` | Logic bắt buộc server kiểm tra lại, không tin client |

## Công cụ dev/QA (tuỳ chọn)

```bash
dotnet run --project src/CineBook.App -- --page=signup
dotnet run --project src/CineBook.App -- --page=movie:m01
dotnet run --project src/CineBook.App -- "--page=booking:st|m01|cgv-1|20261116|4@2:F6,E3" --shot=out.png --delay=5000
```

`--page=` mở thẳng một trang (`home|auth|signup|cinemas|categories|movie:<id>|booking:<showtimeId>[@bước[:ghế,ghế]]|tickets`);
`--shot=` chụp cửa sổ ra PNG rồi thoát (dùng cho kiểm tra giao diện tự động).

## Giả định (quyết định nhỏ đã tự chọn)

- **.NET 10** (`net10.0-windows`) thay vì .NET 8: máy dev hiện chỉ cài .NET 10 runtime, và app Villa trong cùng solution cũng dùng .NET 10.
  Đổi về .NET 8 chỉ cần sửa `TargetFramework`.
- Tên phim trong mock là **tên hư cấu**; poster là gradient + tên phim (không dùng poster có bản quyền). Logo hãng chỉ là chữ + màu badge.
- Thư mục dự án: `CINEMA_CENTER/src/CineBook.App` (project/namespace `CineBook`) – gắn vào cấu trúc chung của repo `DOAN`.
- Giữ ghế được gọi khi bấm **"Giữ ghế & tiếp tục"** ở bước ② (không giữ ngay khi vừa chọn ghế) để giảm số lần gọi API;
  đồng hồ 10:00 chạy từ lúc đó. Nếu backend muốn giữ ghế theo từng lần bấm, chỉ cần đổi `ToggleSeat` trong `BookingViewModel`.
- Ngày sinh khi đăng ký nhận 8 chữ số và tự định dạng thành `dd/MM/yyyy`.
- Không bắt buộc đăng nhập để đặt vé trong bản demo; backend nên yêu cầu token ở `POST /api/bookings`.
- "Nhắc tôi", Google/Facebook và Quên mật khẩu hiện chỉ hiện toast.
- Mã QR trên vé là **placeholder** (không quét được).
- Trang chủ ưu tiên thành phố lưu trong tài khoản khi thành phố đó có trong danh mục demo. Khi chưa chọn, vị trí mặc định của bản demo là TP.HCM.
