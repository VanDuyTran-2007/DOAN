namespace CineBook.Models;

public record AccountProfile(
    string Id, string FullName, string Email, string Phone, string? SecondaryEmail1, string? SecondaryEmail2,
    string City, string District, DateOnly? BirthDate, string MemberTier, decimal TotalSpent, bool HasAvatar,
    DateTimeOffset? AvatarCooldownUntil, int AvatarChangesRemaining, bool PasswordCheckLocked,
    bool RequiresPasswordSetup = false, IReadOnlyList<string>? LinkedProviders = null)
{
    private MemberTier Tier => MemberPricing.ReadTier(MemberTier);
    public string Rank => MemberPricing.DisplayName(Tier);
    public int PurchaseDiscountPercent => MemberPricing.DiscountPercent(Tier);
    public string PurchaseBenefit => PurchaseDiscountPercent > 0
        ? $"Giảm {PurchaseDiscountPercent}% trên tổng tiền vé và combo"
        : "Giá tiêu chuẩn khi mua vé và combo";
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
