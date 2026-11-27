# Đăng nhập Google và Facebook

Google và Facebook mở trong trình duyệt mặc định. CineBook xác thực nhà cung cấp trên máy chủ và trả kết quả về WPF. Khi một danh tính chưa liên kết đăng nhập lần đầu, CineBook nhận tên và email từ nhà cung cấp, sau đó mở **Chi tiết tài khoản** để đặt mật khẩu ban đầu. Ngày sinh, số điện thoại, thành phố và quận để trống. Sau khi đặt mật khẩu, có thể dùng mật khẩu CineBook hoặc nhà cung cấp đã liên kết để đăng nhập.

Nếu email đã tồn tại trong CineBook, đăng nhập bằng mật khẩu CineBook, sau đó chọn **Tài khoản → Chi tiết tài khoản → Liên kết Google/Facebook**. Danh tính đã liên kết với một tài khoản CineBook chỉ đăng nhập được vào tài khoản đó.

## Cấu hình máy chủ

1. Áp dụng migration trên database CineBook, rồi khởi động lại API.

   ```powershell
   dotnet run --project src/CineBook.Api --no-launch-profile -- --init-db
   ```

2. Trong Google Cloud, tạo OAuth client dạng **Web application**. Trong Meta for Developers, tạo ứng dụng có Facebook Login và quyền đọc email. Thêm đúng địa chỉ callback của API vào danh sách redirect URI của từng nhà cung cấp.

   | Nhà cung cấp | Khi chạy HTTPS cục bộ | Khi chạy qua Tailscale |
   |---|---|---|
   | Google | `https://localhost:7085/signin-google` | `https://hoangsang.tail50798d.ts.net/signin-google` |
   | Facebook | `https://localhost:7085/signin-facebook` | `https://hoangsang.tail50798d.ts.net/signin-facebook` |

   Nếu ứng dụng nhà cung cấp ở chế độ thử nghiệm, cấp quyền cho các tài khoản thử theo cài đặt của nhà cung cấp. Chính sách về miền có thể yêu cầu xác minh khi mở ứng dụng cho người dùng ngoài nhóm thử. [Google OAuth cho ứng dụng web](https://developers.google.com/identity/protocols/oauth2/web-server) · [Thiết lập Facebook Login](https://learn.microsoft.com/aspnet/core/security/authentication/social/facebook-logins)

3. Với profile **Local** hoặc **Tailscale Host** trong Visual Studio, lưu thông tin vào User Secrets trên máy chạy API. Có thể cấu hình từng nhà cung cấp độc lập.

   ```powershell
   dotnet user-secrets --project src/CineBook.Api set "Authentication:Google:ClientId" "<Google Client ID>"
   dotnet user-secrets --project src/CineBook.Api set "Authentication:Google:ClientSecret" "<Google Client Secret>"
   dotnet user-secrets --project src/CineBook.Api set "Authentication:Facebook:AppId" "<Facebook App ID>"
   dotnet user-secrets --project src/CineBook.Api set "Authentication:Facebook:AppSecret" "<Facebook App Secret>"
   ```

   Khi triển khai ngoài môi trường Development, cấu hình tương ứng bằng biến môi trường máy chủ, ví dụ `Authentication__Google__ClientId`. Máy thành viên chỉ cần URL API HTTPS và quyền truy cập mạng riêng. Các khóa của Google và Meta chỉ đặt trên máy chủ.
