using CineBook.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CineBook.ViewModels;

/// <summary>
/// Một ghế trên sơ đồ. Bọc <see cref="Seat"/> và bổ sung toạ độ vẽ (Left/Width) để View không phải tính toán.
/// Trạng thái Selected chỉ tồn tại phía client.
/// </summary>
public partial class SeatItemViewModel : ObservableObject
{
    /// <summary>Kích thước 1 ô lưới (px). Ghế đôi chiếm 2 ô.</summary>
    public const double Cell = 38;
    private const double Gap = 6;

    public SeatItemViewModel(Seat seat)
    {
        Seat = seat;
        _status = seat.Status;
    }

    public Seat Seat { get; }
    public string Label => Seat.Label;
    public int Number => Seat.Number;
    public SeatType Type => Seat.Type;
    public decimal Price => Seat.Price;

    /// <summary>Vị trí ngang trong hàng: cột bắt đầu × kích thước ô. Cột bỏ trống = lối đi.</summary>
    public double Left => (Seat.GridColumn - 1) * Cell;
    public double Width => Seat.ColumnSpan * Cell - Gap;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsInteractive))] private SeatStatus _status;

    /// <summary>Chỉ ghế trống hoặc ghế mình đang chọn mới bấm được.</summary>
    public bool IsInteractive => Status is SeatStatus.Available or SeatStatus.Selected;

    public string Tooltip => $"Ghế {Label} · {Labels.SeatType(Type)} · {Labels.Money(Price)}";
}

public class SeatRowViewModel
{
    public required string Label { get; init; }
    public required IReadOnlyList<SeatItemViewModel> Seats { get; init; }
    /// <summary>Chiều rộng canvas của hàng = số cột × ô.</summary>
    public double Width { get; init; }
}

/// <summary>Combo bắp nước kèm số lượng đang chọn.</summary>
public partial class ComboItemViewModel : ObservableObject
{
    public ComboItemViewModel(Combo combo) => Combo = combo;
    public Combo Combo { get; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LineTotal))] private int _quantity;
    public decimal LineTotal => Combo.Price * Quantity;
}

public partial class PaymentOption : ObservableObject
{
    public required PaymentMethod Method { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    [ObservableProperty] private bool _isSelected;
}

public partial class StepItem : ObservableObject
{
    public required int Number { get; init; }
    public required string Title { get; init; }
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isDone;
}
