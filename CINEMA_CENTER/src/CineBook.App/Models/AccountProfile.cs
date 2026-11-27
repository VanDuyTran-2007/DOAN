namespace CineBook.Models;

public record AccountProfile(
    string Id, string FullName, string Email, string Phone, string? SecondaryEmail1, string? SecondaryEmail2,
    string City, string District, DateOnly? BirthDate, string MemberTier, decimal TotalSpent, bool HasAvatar,
    DateTimeOffset? AvatarCooldownUntil, int AvatarChangesRemaining, bool PasswordCheckLocked)
{
    public string Rank => MemberTier.ToLowerInvariant() switch
    {
        "owner" => "Owner",
        "admin" => "Admin",
        "vip" or "silver" or "gold" or "platinum" => "VIP",
        _ => "Thành viên"
    };
    public string DateOfBirth => BirthDate?.ToString("dd/MM/yyyy") ?? "Chưa cung cấp";
    public string Spending => Labels.Money(TotalSpent);
    public string AvatarStatus => AvatarCooldownUntil is { } until
        ? $"Bạn có thể đổi ảnh lại sau {until.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}."
        : $"Còn {AvatarChangesRemaining} lượt đổi ảnh trước khi chờ 24 giờ.";
}

public record AccountChanges(string FullName, string Email, string Phone, string? SecondaryEmail1,
    string? SecondaryEmail2, string City, string District);

public record AccountVoucher(string Code, string Title, string Description, DateTime EndsAtUtc)
{
    public string ExpiryText => $"Hết hạn {EndsAtUtc.AddHours(7):HH:mm dd/MM/yyyy} (giờ Việt Nam)";
}
