# Dùng CineBook với database chung

`CineBook.Api` là API có sẵn của dự án. Khi cả nhóm dùng cùng một địa chỉ API, mọi thao tác đăng ký, đăng nhập và cập nhật hồ sơ trên WPF dùng chung SQL Server mà máy chủ API kết nối tới. Các máy chạy WPF chỉ cần địa chỉ HTTPS của API. Quản trị viên dùng SSMS kết nối trực tiếp tới SQL Server qua mạng riêng hoặc VPN.

## Demo khi nhóm chỉ có máy cá nhân

Chọn một máy chạy cả API và SQL Server. Nếu nhóm demo trực tiếp và đã có HTTPS nội bộ, dùng địa chỉ nội bộ của máy đó. Nếu các thành viên ở xa nhau, nhờ người quản trị mạng của nhóm hoặc giảng viên cấp một mạng riêng. Một lựa chọn để tham khảo là [Tailscale Serve](https://tailscale.com/docs/use-cases/application-testing/share-local-dev-server-with-team), dùng proxy HTTPS trên chính máy chạy API.

### Start trong Visual Studio

Mở `CINEMA_CENTER.slnx` và chọn profile bên cạnh nút Start. Nếu solution đang mở từ trước, đóng rồi mở lại để Visual Studio đọc các profile mới.

| Profile | Dùng ở đâu | Khi bấm Start |
|---|---|---|
| **CineBook - Local** | Máy đang test riêng | Chạy API và WPF với cấu hình localhost |
| **CineBook - Tailscale Host** | Máy có SQL Server và đang chia sẻ API | Chạy API nội bộ cổng 5085 và WPF trỏ tới URL Tailscale |
| **CineBook - Tailscale Client** | Máy của thành viên đã kết nối Tailscale | Chạy WPF với URL Tailscale |

Trên **máy chủ**, giữ Tailscale kết nối và chạy `tailscale serve http://127.0.0.1:5085` trong PowerShell Administrator. Sau đó chọn **CineBook - Tailscale Host** và bấm Start. Nếu đã cấu hình Serve chạy nền, kiểm tra bằng `tailscale serve status`. Máy thành viên chọn **CineBook - Tailscale Client** và bấm Start.

URL mặc định của nhóm lưu trong `src/CineBook.App/Properties/launchSettings.json`, ở trường `CineBook__SharedApiBaseUrl` của profile `Tailscale`. Khi chuyển host chính thức, cập nhật trường này để cả nhóm nhận URL mới sau khi pull. Để thử API khác trên một máy, đặt biến môi trường `CineBook__ApiBaseUrl` trước khi khởi động WPF. Biến này được ưu tiên hơn URL mặc định của nhóm.

### Chạy bằng PowerShell

1. Trên máy chủ demo, nếu database chưa được khởi tạo, chạy lệnh `--init-db` theo README. Mở PowerShell tại thư mục dự án và giữ tiến trình API chạy.

   ```powershell
   dotnet run --project src/CineBook.Api --launch-profile tailscale
   ```

2. Nếu người quản trị đã cấu hình Tailscale trên máy chủ và các máy thử, trên máy chủ chạy lệnh sau từ PowerShell có quyền Administrator.

   ```powershell
   tailscale serve http://127.0.0.1:5085
   ```

   Dùng URL HTTPS mà Serve in ra. Người quản trị cấp quyền tới HTTPS của máy chủ cho đúng thành viên nhóm. SQL Server và cổng HTTP `5085` tiếp tục ở máy chủ. Giữ cửa sổ Serve mở khi demo và bấm **Ctrl+C** khi kết thúc. Xem [lệnh Serve](https://tailscale.com/docs/reference/tailscale-cli/serve) và [quyền truy cập khi chia sẻ máy](https://tailscale.com/docs/features/sharing).
3. Trên máy đồng nghiệp đã được cấp quyền, mở `https://<địa-chỉ-api>/api/movies` để kiểm tra kết nối, rồi chạy `dotnet run --project src/CineBook.App --launch-profile Tailscale`. Máy chủ cần bật và API cần chạy trong lúc demo.

## Triển khai cho nhóm

1. Chọn một máy chủ mà nhóm có quyền sử dụng, ví dụ máy chủ Windows/IIS của nhóm trong mạng riêng hoặc VPN. Chuẩn bị tên máy chủ và chứng chỉ HTTPS mà các máy WPF tin cậy. SQL Server có thể nằm cùng máy hoặc trong cùng mạng riêng. Xem [hướng dẫn triển khai ASP.NET Core với IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0).
2. Từ thư mục dự án, build bản triển khai bằng PowerShell.

   ```powershell
   dotnet publish src/CineBook.Api -c Release
   ```

   Thư mục mặc định là `src/CineBook.Api/bin/Release/net10.0/publish`. Đưa thư mục này lên máy chủ API.
3. Trên máy chủ, đặt biến `ConnectionStrings__DefaultConnection` cho cả tiến trình khởi tạo và tiến trình chạy API. Dùng tên SQL Server và database chung đã chọn. Lưu thông tin đăng nhập SQL trong cấu hình hoặc kho bí mật trên máy chủ.

   Từ thư mục đã publish, chạy một lần bằng tài khoản có quyền áp dụng migration.

   ```powershell
   dotnet .\CineBook.Api.dll --init-db
   ```

   Sau đó chạy API dưới IIS hoặc dịch vụ có địa chỉ HTTPS cố định. Tài khoản dịch vụ API cần quyền dữ liệu mà ứng dụng sử dụng. Khi nâng cấp schema, thực hiện lại lệnh khởi tạo bằng tài khoản có quyền migration. Nếu muốn tiếp tục dùng dữ liệu đã có, trỏ API tới đúng database đó hoặc phục hồi bản sao database vào máy chủ đã chọn trước khi chạy migration.
4. Từ máy đồng nghiệp, kiểm tra `https://<địa-chỉ-api>/api/movies` trả về danh sách phim. Sau đó cấu hình WPF bằng PowerShell trong thư mục dự án.

   ```powershell
   $env:CineBook__ApiBaseUrl = 'https://api-cinebook.example.com/'
   dotnet run --project src/CineBook.App
   ```

   Thay URL ví dụ bằng URL thật. Đồng nghiệp có thể đăng ký tài khoản qua WPF khi đã được cấp quyền truy cập mạng tới API. Biến môi trường ở ví dụ áp dụng cho phiên PowerShell hiện tại và các chương trình được khởi chạy từ đó.

## Xem dữ liệu trong SSMS

1. Kết nối SSMS tới **SQL Server mà API đang dùng** và chọn đúng database. Nếu API chạy trên máy của bạn và chuỗi kết nối vẫn dùng `Server=localhost;Database=CineBookDB`, bạn có thể mở `localhost → CineBookDB` trong SSMS trên máy đó.
2. Mở [CINEBOOK_MONITOR.sql](CINEBOOK_MONITOR.sql) trong một cửa sổ query. Kết quả đầu tiên hiển thị tên SQL Server, tên database và thời điểm đọc. Các kết quả tiếp theo hiển thị số tài khoản, vé đã thanh toán, voucher đang hiệu lực và những hồ sơ gần nhất theo phiên bản dòng.
3. Khi người khác đăng ký hoặc lưu hồ sơ, bấm **F5** để chạy lại truy vấn. SQL Server đọc trạng thái đã commit tại thời điểm truy vấn. Lưới kết quả SSMS giữ nguyên ảnh chụp của lần chạy trước cho đến khi truy vấn lại.

Nếu muốn theo dõi tự động trong lúc demo, chạy [CINEBOOK_WATCH.sql](CINEBOOK_WATCH.sql) và xem tab **Messages**. Truy vấn đọc lại mỗi 5 giây trong khoảng 1 phút. Phiên bản hồ sơ tăng khi dòng tài khoản được ghi mới hoặc cập nhật. Để xem chi tiết, chạy lại truy vấn ở bước 2.

URL API dành cho WPF. Với đồng nghiệp cần xem trực tiếp trong SSMS, quản trị viên SQL cấp một danh tính riêng và quyền đọc trên các bảng hoặc view họ cần, sau đó cung cấp tên SQL Server qua mạng riêng hoặc VPN.

## Phép thử đồng bộ

- Trên máy A, dùng WPF đăng ký một tài khoản thử và chọn thành phố trong **Chi tiết tài khoản**.
- Trên máy B, mở WPF với cùng URL API và đăng nhập bằng tài khoản thử. Tải lại hồ sơ để thấy thành phố đã lưu.
- Trong SSMS, chạy lại truy vấn theo dõi để thấy tài khoản thử và phiên bản hồ sơ mới. Nếu máy B đổi thành phố rồi lưu, lần đọc tiếp theo trên máy A và trong SSMS sẽ thấy giá trị mới.

Đăng ký, đăng nhập, hồ sơ, ảnh đại diện và danh sách voucher đã dùng API. Luồng đặt vé trong WPF vẫn dùng mock. API trả dữ liệu hiện tại khi được gọi, nên các trang đã mở cần tải lại để nhận thay đổi. Giao diện và API phân quyền quản trị là phần việc riêng nếu nhóm muốn quản lý trong CineBook thay vì SSMS.
