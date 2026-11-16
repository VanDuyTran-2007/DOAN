using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DOAN.Converters;
using DOAN.Data;
using DOAN.Services;
using DOAN.ViewModels;

namespace DOAN.Pages
{
    public partial class RoomsPage : Page
    {
        public RoomsPage()
        {
            InitializeComponent();
            DataContext = new RoomsViewModel();
            BannerImage.Source = (ImageSource?)new UrlToImageConverter()
                .Convert(VillaData.HeroImages[1], typeof(ImageSource), "1920", null!);
            Loaded += (_, _) =>
            {
                // Quay lại trang này: cập nhật theo ngày có thể đã đổi ở trang chi tiết
                if (DataContext is RoomsViewModel vm) vm.SyncFromState();
                Navigator.ReportScroll(Scroller.VerticalOffset);
            };
        }

        // Không cho trang tự cuộn khi một ô nhập (DatePicker...) nhận focus lúc mở trang
        private void NoAutoScroll(object sender, RequestBringIntoViewEventArgs e) => e.Handled = true;

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
            => Navigator.ReportScroll(e.VerticalOffset);

        private void Room_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: RoomCardViewModel card }) Navigator.GoDetail(card.Type);
        }

        private void Detail_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is FrameworkElement { DataContext: RoomCardViewModel card }) Navigator.GoDetail(card.Type);
        }

        private void Home_Click(object sender, RoutedEventArgs e) => Navigator.GoHome();
    }
}
