# CineBook – CINEMA_CENTER (Frontend WPF)

App đặt vé xem phim đa hệ thống rạp (CGV, Lotte Cinema, BHD Star, Cinestar). Thư mục này **chỉ chứa phần frontend**
(UI + ViewModel + dữ liệu giả). Backend/database do team khác làm – xem [docs/BACKEND_NOTES.md](docs/BACKEND_NOTES.md).

## Cách chạy

Yêu cầu: Visual Studio 2022/2026 (workload *.NET desktop development*) hoặc .NET SDK 10, Windows 10/11.

```bash
dotnet run --project CINEMA_CENTER/src/CineBook.App
```

Hoặc mở `CINEMA_CENTER/CINEMA_CENTER.slnx` (chỉ app này) hay `Solution/DOAN.slnx` (cả 2 app) trong Visual Studio và nhấn F5.

Dữ liệu demo: đăng nhập bằng email/SĐT bất kỳ, mật khẩu bất kỳ (trừ `wrong-password` để thử trạng thái lỗi).

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
| Trang chủ | Hero carousel tự chuyển 6s, hàng phim cuộn ngang, rạp gần bạn (lọc theo hãng), skeleton/empty/error |
| Chi tiết phim | Thanh chọn 7 ngày → lọc hãng/thành phố → rạp + giờ chiếu theo định dạng |
| Rạp chiếu | Lọc hãng/thành phố/quận; chọn rạp xem phim + lịch chiếu |
| Danh mục / Phim | Lọc thể loại, trạng thái, định dạng, sắp xếp; "Tải thêm" |
| Đăng nhập / Đăng ký | Nền poster trôi chậm, panel kính mờ, validation `INotifyDataErrorInfo` |
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
- Ngày sinh khi đăng ký nhập tay dạng `dd/MM/yyyy` (chưa có DatePicker theo theme).
- Không bắt buộc đăng nhập để đặt vé trong bản demo; backend nên yêu cầu token ở `POST /api/bookings`.
- Trailer, "Nhắc tôi", Google/Facebook, Quên mật khẩu, trang Tài khoản: chỉ hiện toast (chưa có backend).
- Mã QR trên vé là **placeholder** (không quét được).
- Vị trí: mock luôn trả TP.HCM; người dùng đổi thành phố thủ công ở Header (kể cả khi bị từ chối quyền vị trí).
