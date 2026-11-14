using DOAN.Models;
using DOAN.Pages;

namespace DOAN.Services;

/// <summary>Điều hướng giữa các trang trong Frame của MainWindow.</summary>
public static class Navigator
{
    public static MainWindow? Main { get; set; }

    public static void GoHome(string? section = null)
    {
        if (Main == null) return;
        if (Main.MainFrame.Content is HomePage home)
        {
            if (section == null) home.ScrollToTop();
            else home.ScrollToSection(section);
            return;
        }
        Main.MainFrame.Navigate(new HomePage(section));
    }

    public static void GoRooms() => Main?.MainFrame.Navigate(new RoomsPage());

    public static void GoDetail(RoomType type) => Main?.MainFrame.Navigate(new RoomDetailPage(type));

    public static void GoBack()
    {
        if (Main?.MainFrame.CanGoBack == true) Main.MainFrame.GoBack();
        else GoHome();
    }

    /// <summary>Các trang gọi khi cuộn để thanh menu chuyển từ trong suốt sang nền đậm.</summary>
    public static void ReportScroll(double offset) => Main?.SetNavSolid(offset > 60);
}
