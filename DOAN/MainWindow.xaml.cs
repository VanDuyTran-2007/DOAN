using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using DOAN.Data;
using DOAN.Helpers;
using DOAN.Pages;
using DOAN.Services;

namespace DOAN
{
    public partial class MainWindow : Window
    {
        private bool _navSolid;

        public MainWindow()
        {
            InitializeComponent();
            Navigator.Main = this;
            HotlineText.Text = VillaData.Hotline;
            MainFrame.Navigate(new HomePage());

            // Mở thẳng một trang khi chạy thử: DOAN.exe --page rooms | --page deluxe
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--page");
            if (i > 0 && i + 1 < args.Length)
            {
                if (args[i + 1] == "rooms") Navigator.GoRooms();
                else if (VillaData.Find(args[i + 1]) is { } room) Navigator.GoDetail(room);
            }
        }

        /// <summary>Thanh menu trong suốt khi ở đầu trang, chuyển nền xanh đậm khi cuộn xuống.</summary>
        public void SetNavSolid(bool solid)
        {
            if (solid == _navSolid) return;
            _navSolid = solid;
            NavBg.BeginAnimation(OpacityProperty,
                new DoubleAnimation(solid ? 0.97 : 0, new Duration(TimeSpan.FromMilliseconds(250))));
            NavLine.Opacity = solid ? 0 : 1;
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            SetNavSolid(false);
            if (e.Content is FrameworkElement page) Fx.FadeIn(page);

            // Đánh dấu mục menu đang chọn
            foreach (var b in new[] { NavHome, NavAbout, NavRooms, NavAmenities, NavGallery, NavContact })
                b.Tag = null;
            if (e.Content is HomePage) NavHome.Tag = "Active";
            else NavRooms.Tag = "Active";
        }

        private void Logo_Click(object sender, RoutedEventArgs e) => Navigator.GoHome();
        private void NavHome_Click(object sender, RoutedEventArgs e) => Navigator.GoHome();
        private void NavRooms_Click(object sender, RoutedEventArgs e) => Navigator.GoRooms();

        private void NavSection_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.CommandParameter is string section)
                Navigator.GoHome(section);
        }
    }
}
