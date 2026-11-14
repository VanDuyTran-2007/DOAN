namespace DOAN.Services;

/// <summary>Thông tin tìm phòng dùng chung giữa các trang (ngày nhận/trả, số khách).</summary>
public static class AppState
{
    public static DateTime CheckIn { get; set; } = DateTime.Today;
    public static DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);
    public static int Guests { get; set; } = 2;

    public static int Nights => Math.Max(1, (CheckOut - CheckIn).Days);
}
