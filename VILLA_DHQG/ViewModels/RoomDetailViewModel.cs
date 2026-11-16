using System.Collections.ObjectModel;
using System.Globalization;
using DOAN.Data;
using DOAN.Models;
using DOAN.Services;

namespace DOAN.ViewModels;

public class ThumbViewModel : ObservableObject
{
    private bool _isSelected;
    public ThumbViewModel(string url, int index) { Url = url; Index = index; }
    public string Url { get; }
    public int Index { get; }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
}

public record UnitViewModel(string Number, int Floor, bool IsFree)
{
    public string FloorText => Floor > 0 ? $"Tầng {Floor}" : "";
    public string StatusText => IsFree ? "Trống" : "Đã đặt";
}

/// <summary>Một ô trong lịch 14 ngày: số phòng còn trống của hạng phòng trong ngày đó.</summary>
public record DayViewModel(DateTime Date, int Free, int Total, bool InStay)
{
    private static readonly CultureInfo Vi = new("vi-VN");
    public string DayOfWeek => Date.DayOfWeek == System.DayOfWeek.Sunday ? "CN" : "T" + ((int)Date.DayOfWeek + 1);
    public string DayText => Date.ToString("dd", Vi);
    public string MonthText => "Th" + Date.Month;
    public string FreeText => Free == 0 ? "Hết" : $"{Free}/{Total}";
    /// <summary>None = hết phòng, Few = còn ít, Many = còn nhiều.</summary>
    public string Level => Free == 0 ? "None" : Free * 3 <= Total ? "Few" : "Many";
}

public class RoomDetailViewModel : ObservableObject
{
    private int _selectedIndex;
    private DateTime? _checkIn = AppState.CheckIn;
    private DateTime? _checkOut = AppState.CheckOut;
    private int _guests = AppState.Guests;

    public RoomDetailViewModel(RoomType type)
    {
        Type = type;
        Thumbs = type.Images.Select((u, i) => new ThumbViewModel(u, i)).ToList();
        OtherRooms = VillaData.RoomTypes.Where(r => r.Id != type.Id).Take(3).ToList();
        SelectImage(0);
        Recalculate();
    }

    public RoomType Type { get; }
    public List<ThumbViewModel> Thumbs { get; }
    public List<RoomType> OtherRooms { get; }
    public List<int> GuestOptions { get; } = Enumerable.Range(1, 10).ToList();
    public ObservableCollection<UnitViewModel> Units { get; } = [];
    public ObservableCollection<DayViewModel> Days { get; } = [];
    public DateTime Today => DateTime.Today;

    // ---------- Thư viện ảnh ----------
    public string SelectedImage => Type.Images.Count > 0 ? Type.Images[_selectedIndex] : "";
    public string ImageCounter => $"{_selectedIndex + 1} / {Type.Images.Count}";

    public void SelectImage(int index)
    {
        if (Type.Images.Count == 0) return;
        _selectedIndex = (index % Type.Images.Count + Type.Images.Count) % Type.Images.Count;
        foreach (var t in Thumbs) t.IsSelected = t.Index == _selectedIndex;
        OnPropertyChanged(nameof(SelectedImage));
        OnPropertyChanged(nameof(ImageCounter));
    }
    public void NextImage() => SelectImage(_selectedIndex + 1);
    public void PrevImage() => SelectImage(_selectedIndex - 1);

    // ---------- Đặt phòng ----------
    public DateTime? CheckIn
    {
        get => _checkIn;
        set
        {
            if (!Set(ref _checkIn, value) || value is null) return;
            AppState.CheckIn = value.Value;
            if (_checkOut is null || _checkOut <= value) CheckOut = value.Value.AddDays(1);
            else Recalculate();
            OnPropertyChanged(nameof(MinCheckOut));
        }
    }

    public DateTime? CheckOut
    {
        get => _checkOut;
        set
        {
            if (value is not null && _checkIn is not null && value <= _checkIn) value = _checkIn.Value.AddDays(1);
            if (!Set(ref _checkOut, value) || value is null) return;
            AppState.CheckOut = value.Value;
            Recalculate();
        }
    }

    public DateTime MinCheckOut => (_checkIn ?? DateTime.Today).AddDays(1);

    public int Guests
    {
        get => _guests;
        set { if (Set(ref _guests, value)) { AppState.Guests = value; Recalculate(); } }
    }

    public int Available { get; private set; }
    public int Nights { get; private set; }
    public bool IsAvailable => Available > 0;
    public bool CanBook => IsAvailable && !TooManyGuests;
    public bool TooManyGuests => _guests > Type.MaxGuests;
    public string StatusText => Available == 0 ? "Hết phòng trong ngày đã chọn" : $"Còn {Available}/{Type.TotalRooms} phòng trống";
    public string NightsText => $"{Type.PriceText} × {Nights} đêm";
    public string TotalText => $"{Type.Price * Nights:N0} đ";
    public string GuestWarning => $"Hạng phòng này tối đa {Type.MaxGuests} khách. Vui lòng chọn thêm phòng hoặc hạng phòng lớn hơn.";

    private void Recalculate()
    {
        var from = (_checkIn ?? DateTime.Today).Date;
        var to = (_checkOut ?? from.AddDays(1)).Date;
        Nights = Math.Max(1, (to - from).Days);
        Available = Type.CountAvailable(from, to);

        Units.Clear();
        foreach (var r in Type.Rooms)
            Units.Add(new UnitViewModel(r.Number, r.Floor, r.IsFree(from, to)));

        // Lịch 14 ngày bắt đầu từ ngày nhận phòng
        Days.Clear();
        for (int i = 0; i < 14; i++)
        {
            var d = from.AddDays(i);
            Days.Add(new DayViewModel(d, Type.Rooms.Count(r => r.IsFreeOn(d)), Type.TotalRooms, d >= from && d < to));
        }

        foreach (var p in new[] { nameof(Available), nameof(Nights), nameof(IsAvailable), nameof(CanBook),
                                  nameof(TooManyGuests), nameof(StatusText), nameof(NightsText), nameof(TotalText) })
            OnPropertyChanged(p);
    }
}
