using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DOAN.Converters;
using DOAN.Data;
using DOAN.Models;
using DOAN.Services;

namespace DOAN.Pages
{
    public partial class HomePage : Page
    {
        private readonly UrlToImageConverter _img = new();
        private readonly DispatcherTimer _slideTimer = new() { Interval = TimeSpan.FromSeconds(7) };
        private readonly string? _initialSection;
        private int _slide;
        private bool _aIsFront = true;

        // Dữ liệu cho binding trong XAML
        public IReadOnlyList<string> HeroImages => VillaData.HeroImages;
        public IReadOnlyList<GalleryItem> Gallery => VillaData.Gallery;
        public IReadOnlyList<Feature> Features => VillaData.Features;
        public IReadOnlyList<Service> Services => VillaData.Services;
        public IEnumerable<RoomType> FeaturedRooms => VillaData.RoomTypes.Where(r => r.Id is "deluxe" or "family" or "villa");

        public HomePage(string? section = null)
        {
            InitializeComponent();
            DataContext = this;
            _initialSection = section;

            StatRooms.Text = VillaData.RoomTypes.Sum(r => r.TotalRooms).ToString();
            StatTypes.Text = VillaData.RoomTypes.Count.ToString();
            AddressText.Text = VillaData.Address;
            PhoneText.Text = "Hotline: " + VillaData.Hotline;
            EmailText.Text = VillaData.Email;
            CallButton.Content = "GỌI " + VillaData.Hotline;

            // Thanh tìm phòng
            GuestsBox.ItemsSource = Enumerable.Range(1, 10).Select(n => $"{n} khách").ToList();
            GuestsBox.SelectedIndex = Math.Clamp(AppState.Guests - 1, 0, 9);
            CheckInPicker.DisplayDateStart = DateTime.Today;
            CheckInPicker.SelectedDate = AppState.CheckIn;
            CheckOutPicker.DisplayDateStart = AppState.CheckIn.AddDays(1);
            CheckOutPicker.SelectedDate = AppState.CheckOut;

            // Slideshow ảnh lớn
            ShowSlide(0, instant: true);
            _slideTimer.Tick += (_, _) => ShowSlide(_slide + 1);

            Loaded += (_, _) =>
            {
                _slideTimer.Start();
                Navigator.ReportScroll(Scroller.VerticalOffset);
                if (_initialSection != null)
                    Dispatcher.BeginInvoke(() => ScrollToSection(_initialSection), DispatcherPriority.Loaded);
            };
            Unloaded += (_, _) => _slideTimer.Stop();
        }

        // ------------------------- Slideshow -------------------------
        private void ShowSlide(int index, bool instant = false)
        {
            int n = VillaData.HeroImages.Count;
            _slide = (index % n + n) % n;
            var source = (ImageSource?)_img.Convert(VillaData.HeroImages[_slide], typeof(ImageSource), "1920", null!);

            var front = _aIsFront ? HeroA : HeroB;
            var back = _aIsFront ? HeroB : HeroA;
            var target = instant ? front : back;
            target.Source = source;
            SlideCounter.Text = $"{_slide + 1:00} / {n:00}";

            // Hiệu ứng zoom chậm (Ken Burns)
            var scale = new ScaleTransform(1, 1);
            target.RenderTransform = scale;
            var zoom = new DoubleAnimation(1, 1.08, TimeSpan.FromSeconds(9));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, zoom);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, zoom);

            if (instant) { front.Opacity = 1; return; }

            var fade = TimeSpan.FromMilliseconds(1200);
            back.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, fade));
            front.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, fade));
            _aIsFront = !_aIsFront;
        }

        private void NextSlide_Click(object sender, RoutedEventArgs e) { ShowSlide(_slide + 1); RestartTimer(); }
        private void PrevSlide_Click(object sender, RoutedEventArgs e) { ShowSlide(_slide - 1); RestartTimer(); }
        private void RestartTimer() { _slideTimer.Stop(); _slideTimer.Start(); }

        // ------------------------- Cuộn trang -------------------------
        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
            => Navigator.ReportScroll(e.VerticalOffset);

        public void ScrollToTop() => SmoothScroll(0);

        public void ScrollToSection(string section)
        {
            FrameworkElement? target = section switch
            {
                "about" => AboutSection,
                "amenities" => AmenitiesSection,
                "gallery" => GallerySection,
                "contact" => ContactSection,
                _ => null,
            };
            if (target == null) return;
            UpdateLayout();
            double y = target.TransformToAncestor(Root).Transform(new Point(0, 0)).Y;
            SmoothScroll(Math.Max(0, y - 90));
        }

        private void SmoothScroll(double to)
        {
            double from = Scroller.VerticalOffset;
            var start = DateTime.Now;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(12) };
            timer.Tick += (_, _) =>
            {
                double t = Math.Min(1, (DateTime.Now - start).TotalMilliseconds / 600);
                double eased = 1 - Math.Pow(1 - t, 3);
                Scroller.ScrollToVerticalOffset(from + (to - from) * eased);
                if (t >= 1) timer.Stop();
            };
            timer.Start();
        }

        // ------------------------- Sự kiện -------------------------
        private void CheckIn_Changed(object? sender, SelectionChangedEventArgs e)
        {
            if (CheckInPicker.SelectedDate is not DateTime d) return;
            CheckOutPicker.DisplayDateStart = d.AddDays(1);
            if (CheckOutPicker.SelectedDate is not DateTime o || o <= d)
                CheckOutPicker.SelectedDate = d.AddDays(1);
        }

        private void CheckAvailability_Click(object sender, RoutedEventArgs e)
        {
            var inDate = CheckInPicker.SelectedDate ?? DateTime.Today;
            var outDate = CheckOutPicker.SelectedDate ?? inDate.AddDays(1);
            if (outDate <= inDate) outDate = inDate.AddDays(1);

            AppState.CheckIn = inDate.Date;
            AppState.CheckOut = outDate.Date;
            AppState.Guests = GuestsBox.SelectedIndex + 1;
            Navigator.GoRooms();
        }

        private void Room_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: RoomType room }) Navigator.GoDetail(room);
        }

        private void ExploreRooms_Click(object sender, RoutedEventArgs e) => Navigator.GoRooms();
        private void About_Click(object sender, RoutedEventArgs e) => ScrollToSection("about");
        private void Amenities_Click(object sender, RoutedEventArgs e) => ScrollToSection("amenities");
        private void Gallery_Click(object sender, RoutedEventArgs e) => ScrollToSection("gallery");
        private void Top_Click(object sender, RoutedEventArgs e) => ScrollToTop();
    }
}
