using System.Collections.ObjectModel;
using System.Windows.Threading;
using CineBook.Models;
using CineBook.Navigation;
using CineBook.Services;
using CineBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CineBook.ViewModels;

/// <summary>
/// Wizard đặt vé: ① Chọn suất → ② Chọn ghế → ③ Bắp nước → ④ Thanh toán → ⑤ Hoàn tất.
/// Mọi giá tiền hiển thị ở đây chỉ để tham khảo – WARNING-BACKEND: server tính lại tổng tiền khi tạo booking.
/// </summary>
public partial class BookingViewModel : ViewModelBase
{
    public const int MaxSeats = 8;

    private readonly IShowtimeService _showtimes;
    private readonly ISeatService _seats;
    private readonly IBookingService _bookings;
    private readonly IMovieService _movies;
    private readonly ICinemaService _cinemas;
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IToastService _toast;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private SeatHold? _hold;
    private SeatMap? _map;

    public BookingViewModel(IShowtimeService showtimes, ISeatService seats, IBookingService bookings, IMovieService movies,
                            ICinemaService cinemas, IAuthService auth, INavigationService nav, IToastService toast)
    {
        _showtimes = showtimes; _seats = seats; _bookings = bookings; _movies = movies; _cinemas = cinemas;
        _auth = auth; _nav = nav; _toast = toast;
        _timer.Tick += (_, _) => OnTick();
        PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IsBusy)) RefreshState(); }; // IsBusy nằm ở lớp cha

        string[] titles = ["Chọn suất", "Chọn ghế", "Bắp nước", "Thanh toán", "Hoàn tất"];
        for (int i = 0; i < titles.Length; i++) Steps.Add(new StepItem { Number = i + 1, Title = titles[i] });
        UpdateSteps();

        PaymentOptions =
        [
            new() { Method = PaymentMethod.Momo, Name = "Ví MoMo", Description = "Quét mã QR bằng ứng dụng MoMo", IsSelected = true },
            new() { Method = PaymentMethod.ZaloPay, Name = "ZaloPay", Description = "Thanh toán qua ví ZaloPay" },
            new() { Method = PaymentMethod.VnPay, Name = "VNPay", Description = "Thanh toán bằng VNPay-QR hoặc ngân hàng nội địa" },
            new() { Method = PaymentMethod.Card, Name = "Thẻ quốc tế", Description = "Visa / MasterCard / JCB" },
        ];
    }

    // ---------- Dữ liệu chung ----------
    public ObservableCollection<StepItem> Steps { get; } = [];
    public ObservableCollection<ShowtimeGroup> OtherShowtimes { get; } = [];
    public ObservableCollection<SeatRowViewModel> Rows { get; } = [];
    public ObservableCollection<SeatItemViewModel> SelectedSeats { get; } = [];
    public ObservableCollection<ComboItemViewModel> Combos { get; } = [];
    public IReadOnlyList<PaymentOption> PaymentOptions { get; }

    [ObservableProperty] private int _currentStep = 1;
    [ObservableProperty] private Showtime? _showtime;
    [ObservableProperty] private Movie? _movie;
    [ObservableProperty] private Cinema? _cinema;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _seatWarning;
    [ObservableProperty] private string _countdownText = "";
    [ObservableProperty] private bool _isHolding;
    [ObservableProperty] private bool _isExpiredDialogOpen;
    [ObservableProperty] private Booking? _completedBooking;
    [ObservableProperty] private string? _paymentError;
    [ObservableProperty] private bool _isSeatMapLoading;

    public string UserName => _auth.CurrentUser?.FullName ?? "Khách";
    public decimal SeatsTotal => SelectedSeats.Sum(s => s.Price);
    public decimal CombosTotal => Combos.Sum(c => c.LineTotal);
    private MemberTier PurchaseTier => _auth.CurrentUser?.MemberTier ?? MemberTier.Standard;
    public decimal Subtotal => SeatsTotal + CombosTotal;
    public decimal MemberDiscount => MemberPricing.DiscountAmount(Subtotal, PurchaseTier);
    public bool HasMemberDiscount => MemberDiscount > 0;
    public string MemberDiscountLabel => $"Ưu đãi {MemberPricing.DisplayName(PurchaseTier)} ({MemberPricing.DiscountPercent(PurchaseTier)}%)";
    public decimal GrandTotal => Subtotal - MemberDiscount;
    public string SelectedSeatsText => string.Join(", ", SelectedSeats.Select(s => s.Label));
    public bool HasSelectedSeats => SelectedSeats.Count > 0;
    public bool HasSelectedCombos => Combos.Any(c => c.Quantity > 0);
    public IEnumerable<ComboItemViewModel> ChosenCombos => Combos.Where(c => c.Quantity > 0);
    public bool CanGoBack => CurrentStep is > 1 and < 5 && !IsBusy;
    public bool CanGoNext => CurrentStep switch
    {
        1 => Showtime is not null,
        2 => SelectedSeats.Count > 0 && SeatWarning is null,
        3 or 4 => true,
        _ => false
    } && !IsBusy;
    public string NextText => CurrentStep switch { 2 => "Giữ ghế & tiếp tục", 4 => "Thanh toán", _ => "Tiếp tục" };
    public bool IsPayStep => CurrentStep == 4;

    // ---------- Khởi tạo ----------
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        _auth.AuthStateChanged += OnAuthStateChanged;
        var args = parameter as BookingArgs;
        IsBusy = true;
        try { await LoadShowtimeAsync(args?.ShowtimeId ?? ""); }
        catch (Exception ex) { LoadError = ex.Message; }
        finally { IsBusy = false; RefreshState(); }

        // Dev/QA: nhảy thẳng tới bước chỉ định (không dùng ở luồng thật).
        if (args is { Step: >= 2 } && LoadError is null)
        {
            await GoToSeatsAsync();
            foreach (var label in (args.DevSeats ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (Rows.SelectMany(r => r.Seats).FirstOrDefault(s => s.Label == label) is { } seat) ToggleSeat(seat);
            if (args.Step >= 3 && HasSelectedSeats) { await HoldAndContinueAsync(); }
            if (args.Step >= 4 && CurrentStep == 3) CurrentStep = 4;
            if (args.Step >= 5 && CurrentStep == 4) await PayAsync();
            RefreshState();
        }
    }

    public override void OnNavigatedFrom()
    {
        _auth.AuthStateChanged -= OnAuthStateChanged;
        _timer.Stop();
        ReleaseHold();
    }

    private void OnAuthStateChanged(object? sender, EventArgs e) => RefreshState();

    private async Task LoadShowtimeAsync(string showtimeId)
    {
        // TODO-BACKEND: GET /api/showtimes/{id} → Showtime; lỗi 404 nếu suất đã qua hoặc không tồn tại.
        var st = await _showtimes.GetByIdAsync(showtimeId) ?? throw new InvalidOperationException("Suất chiếu không tồn tại hoặc đã kết thúc");
        Showtime = st;
        Movie = await _movies.GetByIdAsync(st.MovieId);
        Cinema = (await _cinemas.GetCinemasAsync(new CinemaFilter())).FirstOrDefault(c => c.Id == st.CinemaId);

        // Các suất khác cùng phim, cùng rạp, cùng ngày để người dùng đổi suất ở bước ①.
        var others = await _showtimes.GetShowtimesAsync(st.MovieId, st.CinemaId, st.StartTime.Date);
        OtherShowtimes.Clear();
        foreach (var g in ShowtimeGroup.Build(others)) OtherShowtimes.Add(g);
        // Suất đang chọn có thể đã qua giờ nên không có trong danh sách → thêm vào để vẫn hiển thị.
        if (others.All(o => o.Id != st.Id)) OtherShowtimes.Insert(0, new ShowtimeGroup { Title = st.FormatText, Times = [st] });

        ResetSeats();
    }

    // ---------- Điều hướng wizard ----------
    [RelayCommand]
    private async Task NextAsync()
    {
        if (!CanGoNext) return;
        switch (CurrentStep)
        {
            case 1: await GoToSeatsAsync(); break;
            case 2: await HoldAndContinueAsync(); break;
            case 3: CurrentStep = 4; PaymentError = null; break;
            case 4: await PayAsync(); break;
        }
        RefreshState();
    }

    [RelayCommand]
    private void Back()
    {
        if (!CanGoBack) return;
        CurrentStep--;
        RefreshState();
    }

    private void RefreshState()
    {
        UpdateSteps();
        foreach (var p in new[] { nameof(CanGoBack), nameof(CanGoNext), nameof(NextText), nameof(IsPayStep), nameof(SeatsTotal), nameof(CombosTotal),
                                  nameof(Subtotal), nameof(MemberDiscount), nameof(HasMemberDiscount), nameof(MemberDiscountLabel),
                                  nameof(GrandTotal), nameof(UserName), nameof(SelectedSeatsText), nameof(HasSelectedSeats), nameof(HasSelectedCombos), nameof(ChosenCombos) })
            OnPropertyChanged(p);
        NextCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentStepChanged(int value) => RefreshState();

    private void UpdateSteps()
    {
        foreach (var s in Steps) { s.IsActive = s.Number == CurrentStep; s.IsDone = s.Number < CurrentStep; }
    }

    // ---------- ① Chọn suất ----------
    [RelayCommand]
    private void SelectShowtime(Showtime st)
    {
        if (Showtime?.Id == st.Id) return;
        ReleaseHold();
        IsBusy = true;
        try { Showtime = st; ResetSeats(); }
        finally { IsBusy = false; }
    }

    // ---------- ② Chọn ghế ----------
    private void ResetSeats()
    {
        _map = null;
        Rows.Clear(); SelectedSeats.Clear(); SeatWarning = null;
    }

    private async Task GoToSeatsAsync()
    {
        IsBusy = true; IsSeatMapLoading = true;
        try
        {
            CurrentStep = 2;
            if (_map is null && Showtime is not null)
            {
                // TODO-BACKEND: GET /api/showtimes/{id}/seats → SeatMap (trạng thái Available/Booked/Holding/Unavailable)
                // NOTE-DB: trạng thái ghế = join Seats với BookingSeats + SeatHolds còn hạn của suất chiếu.
                _map = await _seats.GetSeatMapAsync(Showtime.Id);
                Rows.Clear();
                foreach (var r in _map.Rows)
                    Rows.Add(new SeatRowViewModel { Label = r.Label, Seats = r.Seats.Select(s => new SeatItemViewModel(s)).ToList(), Width = _map.TotalColumns * SeatItemViewModel.Cell });
            }
        }
        catch (Exception ex) { _toast.Show("Không tải được sơ đồ ghế: " + ex.Message, ToastKind.Error); CurrentStep = 1; }
        finally { IsBusy = false; IsSeatMapLoading = false; }
    }

    [RelayCommand]
    private void ToggleSeat(SeatItemViewModel seat)
    {
        if (!seat.IsInteractive || IsHolding && CurrentStep != 2) return;

        if (seat.Status == SeatStatus.Selected)
        {
            seat.Status = SeatStatus.Available;
            SelectedSeats.Remove(seat);
        }
        else
        {
            // WARNING-BACKEND: giới hạn tối đa 8 ghế/lần phải kiểm tra lại ở server.
            if (SelectedSeats.Count >= MaxSeats) { _toast.Show($"Chỉ được chọn tối đa {MaxSeats} ghế mỗi lần", ToastKind.Error); return; }
            seat.Status = SeatStatus.Selected;
            SelectedSeats.Add(seat);
        }
        SeatWarning = FindLoneSeatWarning();
        RefreshState();
    }

    /// <summary>
    /// Rule "không để trống lẻ 1 ghế": nếu một ghế trống nằm kẹp giữa 2 ghế đã bị chiếm (đã đặt / đang giữ / đang chọn)
    /// và ít nhất 1 trong 2 ghế đó là ghế mình vừa chọn → cảnh báo.
    /// WARNING-BACKEND: rule này BẮT BUỘC kiểm tra lại ở server khi giữ ghế (client có thể bị bỏ qua).
    /// </summary>
    private string? FindLoneSeatWarning()
    {
        static bool Occupied(SeatItemViewModel s) => s.Status is SeatStatus.Booked or SeatStatus.Holding or SeatStatus.Selected or SeatStatus.Unavailable;
        foreach (var row in Rows)
        {
            var seats = row.Seats.OrderBy(s => s.Seat.GridColumn).ToList();
            for (int i = 1; i < seats.Count - 1; i++)
            {
                var (prev, cur, next) = (seats[i - 1], seats[i], seats[i + 1]);
                // Chỉ tính khi 3 ghế nằm liền nhau (không bị lối đi chen giữa).
                bool contiguous = prev.Seat.GridColumn + prev.Seat.ColumnSpan == cur.Seat.GridColumn
                               && cur.Seat.GridColumn + cur.Seat.ColumnSpan == next.Seat.GridColumn;
                if (!contiguous || cur.Status != SeatStatus.Available) continue;
                if (Occupied(prev) && Occupied(next) && (prev.Status == SeatStatus.Selected || next.Status == SeatStatus.Selected))
                    return $"Không để trống 1 ghế lẻ ({cur.Label}) giữa các ghế đã chọn/đã đặt. Vui lòng chọn lại.";
            }
        }
        return null;
    }

    private async Task HoldAndContinueAsync()
    {
        if (Showtime is null) return;
        IsBusy = true;
        try
        {
            ReleaseHold();
            // TODO-BACKEND: POST /api/showtimes/{id}/holds body {seatIds[]} → SeatHold {holdId, expiresAt}
            // WARNING-BACKEND: 2 người cùng chọn 1 ghế → server trả 409; UI sẽ tải lại sơ đồ ghế.
            _hold = await _seats.HoldSeatsAsync(Showtime.Id, SelectedSeats.Select(s => s.Seat.Id).ToList());
            IsHolding = true;
            _timer.Start();
            OnTick();
            if (Combos.Count == 0) await LoadCombosAsync();
            CurrentStep = 3;
        }
        catch (Exception ex)
        {
            _toast.Show("Không giữ được ghế: " + ex.Message, ToastKind.Error);
        }
        finally { IsBusy = false; }
    }

    private void ReleaseHold()
    {
        _timer.Stop();
        IsHolding = false;
        CountdownText = "";
        if (_hold is null) return;
        var id = _hold.HoldId;
        _hold = null;
        // TODO-BACKEND: DELETE /api/holds/{holdId} → 204 (nhả ghế khi người dùng rời trang / đổi ý)
        _ = _seats.ReleaseSeatsAsync(id);
    }

    // ---------- Đồng hồ giữ ghế ----------
    private void OnTick()
    {
        if (_hold is null) return;
        var left = _hold.ExpiresAt - DateTimeOffset.UtcNow;
        if (left <= TimeSpan.Zero)
        {
            // Hết giờ: nhả ghế, hiện dialog, quay về bước ①.
            ReleaseHold();
            IsExpiredDialogOpen = true;
            return;
        }
        CountdownText = $"{(int)left.TotalMinutes:00}:{left.Seconds:00}";
    }

    [RelayCommand]
    private void ConfirmExpired()
    {
        IsExpiredDialogOpen = false;
        ResetSeats();
        foreach (var c in Combos) c.Quantity = 0;
        CurrentStep = 1;
        RefreshState();
    }

    // ---------- ③ Bắp nước ----------
    private async Task LoadCombosAsync()
    {
        // TODO-BACKEND: GET /api/combos?cinemaId= → List<Combo> (menu có thể khác nhau giữa các rạp/hãng)
        foreach (var c in await _bookings.GetCombosAsync())
        {
            var vm = new ComboItemViewModel(c);
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ComboItemViewModel.Quantity)) RefreshState(); };
            Combos.Add(vm);
        }
    }

    [RelayCommand] private void IncreaseCombo(ComboItemViewModel c) { if (c.Quantity < 10) c.Quantity++; }
    [RelayCommand] private void DecreaseCombo(ComboItemViewModel c) { if (c.Quantity > 0) c.Quantity--; }

    // ---------- ④ Thanh toán ----------
    [RelayCommand]
    private void SelectPayment(PaymentOption option)
    {
        foreach (var p in PaymentOptions) p.IsSelected = p == option;
    }

    private async Task PayAsync()
    {
        if (Showtime is null || _hold is null) return;
        var method = PaymentOptions.First(p => p.IsSelected).Method;
        IsBusy = true; PaymentError = null;
        try
        {
            // TODO-BACKEND: POST /api/bookings body {showtimeId, holdId, seatIds[], combos{id:qty}} → Booking (Pending)
            // WARNING-BACKEND: giá tính lại ở server; dùng Idempotency-Key để chống bấm 2 lần tạo 2 đơn.
            var booking = await _bookings.CreateBookingAsync(new CreateBookingRequest(
                Showtime.Id, _hold.HoldId, SelectedSeats.Select(s => s.Seat.Id).ToList(),
                Combos.Where(c => c.Quantity > 0).ToDictionary(c => c.Combo.Id, c => c.Quantity)));

            // TODO-BACKEND: POST /api/bookings/{id}/pay body {method} → {paymentUrl}. Thực tế mở cổng thanh toán rồi chờ IPN.
            // WARNING-BACKEND: trạng thái Paid chỉ đặt bởi IPN đã xác thực chữ ký của cổng thanh toán.
            var result = await _bookings.ConfirmPaymentAsync(booking.Id, method);
            if (!result.Success) { PaymentError = result.Error ?? "Thanh toán thất bại"; return; }

            CompletedBooking = result.Booking ?? booking;
            _timer.Stop();
            _hold = null; IsHolding = false; CountdownText = ""; // ghế đã thành vé, không cần nhả nữa
            CurrentStep = 5;
            _toast.Show("Thanh toán thành công!", ToastKind.Success);
        }
        catch (Exception ex) { PaymentError = "Lỗi thanh toán: " + ex.Message; }
        finally { IsBusy = false; }
    }

    // ---------- ⑤ Hoàn tất ----------
    [RelayCommand] private void GoHome() => _nav.NavigateTo<HomeViewModel>();
    [RelayCommand] private void GoMyTickets() => _nav.NavigateTo<MyTicketsViewModel>();
    [RelayCommand] private void CloseToMovie() { if (_nav.CanGoBack) _nav.GoBack(); else _nav.NavigateTo<HomeViewModel>(); }
}
