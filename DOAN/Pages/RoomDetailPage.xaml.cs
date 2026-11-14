using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DOAN.Data;
using DOAN.Models;
using DOAN.Services;
using DOAN.ViewModels;

namespace DOAN.Pages
{
    public partial class RoomDetailPage : Page
    {
        private readonly RoomDetailViewModel _vm;

        public RoomDetailPage(RoomType type)
        {
            InitializeComponent();
            _vm = new RoomDetailViewModel(type);
            DataContext = _vm;
            Title = type.Name;
            HotlineText.Text = "☎  " + VillaData.Hotline;
            EmailText.Text = VillaData.Email;
            Loaded += (_, _) => Navigator.ReportScroll(Scroller.VerticalOffset);
        }

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
            => Navigator.ReportScroll(e.VerticalOffset);

        private void PrevImage_Click(object sender, RoutedEventArgs e) => _vm.PrevImage();
        private void NextImage_Click(object sender, RoutedEventArgs e) => _vm.NextImage();

        private void Thumb_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ThumbViewModel t }) _vm.SelectImage(t.Index);
        }

        private void OtherRoom_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: RoomType room }) Navigator.GoDetail(room);
        }

        private void Book_Click(object sender, RoutedEventArgs e)
        {
            // Phần front end: chỉ hiển thị xác nhận. Lưu đặt phòng vào CSDL sẽ làm ở giai đoạn sau.
            MessageBox.Show(
                $"Yêu cầu đặt phòng:\n\n" +
                $"• Hạng phòng: {_vm.Type.Name}\n" +
                $"• Nhận phòng: {_vm.CheckIn:dd/MM/yyyy}  –  Trả phòng: {_vm.CheckOut:dd/MM/yyyy}\n" +
                $"• Số khách: {_vm.Guests}\n" +
                $"• Tổng tiền: {_vm.TotalText}\n\n" +
                "Chức năng đặt phòng sẽ được kết nối cơ sở dữ liệu ở bước tiếp theo.",
                "Villa ĐHQG – Đặt phòng", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Back_Click(object sender, RoutedEventArgs e) => Navigator.GoBack();
        private void Home_Click(object sender, RoutedEventArgs e) => Navigator.GoHome();
        private void Rooms_Click(object sender, RoutedEventArgs e) => Navigator.GoRooms();
    }
}
