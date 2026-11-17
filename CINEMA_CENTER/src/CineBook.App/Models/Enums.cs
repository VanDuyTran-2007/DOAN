namespace CineBook.Models;

// NOTE-DB: các enum nên lưu dạng tinyint/varchar có CHECK constraint; giá trị chuỗi phải khớp docs/BACKEND_NOTES.md.

public enum MovieStatus { NowShowing, ComingSoon }

/// <summary>Nhãn độ tuổi theo quy định Việt Nam.</summary>
public enum AgeRating { P, K, T13, T16, T18 }

public enum MovieFormat { TwoD, ThreeD, IMAX, FourDX }

public enum SubtitleLanguage { Subtitled, Dubbed }

public enum SeatType { Standard, Vip, Couple }

public enum SeatStatus
{
    Available,
    /// <summary>Người dùng hiện tại đang chọn (chỉ tồn tại phía client).</summary>
    Selected,
    Booked,
    /// <summary>Người khác đang giữ chỗ (hold chưa hết hạn).</summary>
    Holding,
    Unavailable
}

public enum MemberTier { Standard, Silver, Gold, Platinum }

public enum BookingStatus { Pending, Paid, Failed, Expired, Refunded }

public enum PaymentMethod { Momo, ZaloPay, VnPay, Card }

public enum MovieSort { Newest, Rating }
