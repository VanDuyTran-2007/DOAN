using CineBook.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CineBook.ViewModels;

/// <summary>Một lựa chọn trong ComboBox lọc (Label hiển thị, Value = null nghĩa là "Tất cả").</summary>
public record FilterOption<T>(string Label, T? Value);

/// <summary>Ô chọn ngày (7 ngày tới) dùng ở trang chi tiết phim và trang rạp.</summary>
public partial class DateOption : ObservableObject
{
    public required DateTime Date { get; init; }
    public required string DayText { get; init; }
    public string DateText => Date.ToString("dd/MM");
    [ObservableProperty] private bool _isSelected;

    private static string DayName(DateTime d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ 2", DayOfWeek.Tuesday => "Thứ 3", DayOfWeek.Wednesday => "Thứ 4",
        DayOfWeek.Thursday => "Thứ 5", DayOfWeek.Friday => "Thứ 6", DayOfWeek.Saturday => "Thứ 7", _ => "CN"
    };

    /// <summary>Tạo danh sách <paramref name="count"/> ngày bắt đầu từ hôm nay (theo giờ máy, backend nên dùng giờ VN).</summary>
    public static List<DateOption> NextDays(int count = 7)
        => Enumerable.Range(0, count).Select(i =>
        {
            var d = DateTime.Today.AddDays(i);
            return new DateOption { Date = d, DayText = i == 0 ? "Hôm nay" : DayName(d) };
        }).ToList();
}

/// <summary>Nhóm các giờ chiếu cùng định dạng, ví dụ "2D Phụ đề".</summary>
public class ShowtimeGroup
{
    public required string Title { get; init; }
    public required IReadOnlyList<Showtime> Times { get; init; }

    public static IReadOnlyList<ShowtimeGroup> Build(IEnumerable<Showtime> showtimes)
        => showtimes.GroupBy(s => (s.Format, s.Language))
                    .OrderBy(g => g.Key.Format).ThenBy(g => g.Key.Language)
                    .Select(g => new ShowtimeGroup
                    {
                        Title = $"{Labels.Format(g.Key.Format)} {Labels.Language(g.Key.Language)}",
                        Times = g.OrderBy(t => t.StartTime).ToList()
                    }).ToList();
}

/// <summary>Lịch chiếu của một rạp (dùng ở trang chi tiết phim).</summary>
public class CinemaShowtimes
{
    public required Cinema Cinema { get; init; }
    public required IReadOnlyList<ShowtimeGroup> Groups { get; init; }
}

/// <summary>Lịch chiếu của một phim tại một rạp (dùng ở trang Rạp chiếu).</summary>
public class MovieShowtimes
{
    public required Movie Movie { get; init; }
    public required IReadOnlyList<ShowtimeGroup> Groups { get; init; }
}
