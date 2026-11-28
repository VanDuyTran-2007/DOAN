namespace CineBook.Models;

public static class MemberPricing
{
    public static MemberTier ReadTier(string? tier) => Enum.TryParse(tier, ignoreCase: true, out MemberTier value)
        ? value
        : MemberTier.Standard;

    public static int DiscountPercent(MemberTier tier) => tier switch
    {
        MemberTier.Silver => 5,
        MemberTier.Gold => 10,
        MemberTier.Platinum => 20,
        MemberTier.Vip => 35,
        _ => 0
    };

    public static decimal DiscountAmount(decimal subtotal, MemberTier tier)
        => Math.Round(subtotal * DiscountPercent(tier) / 100m, 0, MidpointRounding.AwayFromZero);

    public static string DisplayName(MemberTier tier) => tier switch
    {
        MemberTier.Silver => "Silver",
        MemberTier.Gold => "Gold",
        MemberTier.Platinum => "Platinum",
        MemberTier.Vip => "VIP",
        MemberTier.Admin => "Admin",
        MemberTier.Owner => "Owner",
        _ => "Thành viên"
    };
}
