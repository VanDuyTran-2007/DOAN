using System.Collections.ObjectModel;
using DOAN.Data;
using DOAN.Models;
using DOAN.Services;

namespace DOAN.ViewModels;

/// <summary>Một thẻ phòng trong danh sách, kèm số phòng còn trống theo ngày đã chọn.</summary>
public class RoomCardViewModel
{
    public RoomCardViewModel(RoomType type, int available, int guests)
    {
        Type = type;
        Available = available;
        FitsGuests = guests <= type.MaxGuests;
    }

    public RoomType Type { get; }
    public int Available { get; }
    public bool FitsGuests { get; }
    public bool IsAvailable => Available > 0;
    public bool IsFew => Available is > 0 and <= 2;

    public string StatusText => Available switch
    {
        0 => "Hết phòng",
        <= 2 => $"Chỉ còn {Available} phòng",
        _ => $"Còn {Available} phòng trống",
    };
    public string AvailabilityText => $"{Available}/{Type.TotalRooms} phòng trống";
}

public class RoomsViewModel : ObservableObject
{
    private string _search = "";
    private DateTime? _checkIn = AppState.CheckIn;
    private DateTime? _checkOut = AppState.CheckOut;
    private int _guests = AppState.Guests;
    private int _statusIndex;
    private int _sortIndex;
    private string _summary = "";
    private bool _isEmpty;

    public RoomsViewModel() => Refresh();

    public DateTime Today => DateTime.Today;
    public List<int> GuestOptions { get; } = Enumerable.Range(1, 10).ToList();
    public List<string> StatusOptions { get; } = ["Tất cả trạng thái", "Còn trống", "Hết phòng"];
    public List<string> SortOptions { get; } = ["Đề xuất", "Giá tăng dần", "Giá giảm dần", "Sức chứa lớn nhất"];
    public ObservableCollection<RoomCardViewModel> Results { get; } = [];

    public string SearchText { get => _search; set { if (Set(ref _search, value)) Refresh(); } }
    public int StatusIndex { get => _statusIndex; set { if (Set(ref _statusIndex, value)) Refresh(); } }
    public int SortIndex { get => _sortIndex; set { if (Set(ref _sortIndex, value)) Refresh(); } }
    public int Guests { get => _guests; set { if (Set(ref _guests, value)) { AppState.Guests = value; Refresh(); } } }

    public DateTime? CheckIn
    {
        get => _checkIn;
        set
        {
            if (!Set(ref _checkIn, value) || value is null) return;
            AppState.CheckIn = value.Value;
            if (_checkOut is null || _checkOut <= value) CheckOut = value.Value.AddDays(1);
            else Refresh();
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
            Refresh();
        }
    }

    public DateTime MinCheckOut => (_checkIn ?? DateTime.Today).AddDays(1);

    public string Summary { get => _summary; private set => Set(ref _summary, value); }
    public bool IsEmpty { get => _isEmpty; private set => Set(ref _isEmpty, value); }

    /// <summary>Lấy lại ngày/số khách từ AppState (khi người dùng quay lại từ trang chi tiết).</summary>
    public void SyncFromState()
    {
        _checkIn = AppState.CheckIn;
        _checkOut = AppState.CheckOut;
        _guests = AppState.Guests;
        OnPropertyChanged(nameof(CheckIn));
        OnPropertyChanged(nameof(CheckOut));
        OnPropertyChanged(nameof(Guests));
        OnPropertyChanged(nameof(MinCheckOut));
        Refresh();
    }

    public void Refresh()
    {
        var from = (_checkIn ?? DateTime.Today).Date;
        var to = (_checkOut ?? from.AddDays(1)).Date;

        IEnumerable<RoomCardViewModel> q = VillaData.RoomTypes
            .Select(t => new RoomCardViewModel(t, t.CountAvailable(from, to), _guests));

        if (!string.IsNullOrWhiteSpace(_search))
        {
            var s = _search.Trim();
            q = q.Where(c => c.Type.Name.Contains(s, StringComparison.CurrentCultureIgnoreCase)
                          || c.Type.Category.Contains(s, StringComparison.CurrentCultureIgnoreCase)
                          || c.Type.BedType.Contains(s, StringComparison.CurrentCultureIgnoreCase));
        }

        q = q.Where(c => c.FitsGuests);
        q = _statusIndex switch
        {
            1 => q.Where(c => c.IsAvailable),
            2 => q.Where(c => !c.IsAvailable),
            _ => q,
        };
        q = _sortIndex switch
        {
            1 => q.OrderBy(c => c.Type.Price),
            2 => q.OrderByDescending(c => c.Type.Price),
            3 => q.OrderByDescending(c => c.Type.MaxGuests),
            _ => q.OrderByDescending(c => c.IsAvailable),
        };

        var list = q.ToList();
        Results.Clear();
        foreach (var c in list) Results.Add(c);

        int nights = Math.Max(1, (to - from).Days);
        Summary = $"{list.Count} hạng phòng phù hợp  ·  {list.Count(c => c.IsAvailable)} hạng còn trống  ·  " +
                  $"{from:dd/MM} – {to:dd/MM} ({nights} đêm), {_guests} khách";
        IsEmpty = list.Count == 0;
    }
}
