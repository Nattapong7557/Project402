using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.Membership;
using Nattapong_7379_L002.Services.FoodOrdering;

namespace Nattapong_7379_L002.Services.Membership;

public static class MembershipRules
{
    public const string PremiumPackage = "Premium";
    public const decimal PremiumPrice = 299m;

    public static bool CanSelectSignupPackage(string? package) =>
        string.Equals(package, PremiumPackage, StringComparison.OrdinalIgnoreCase);

    public static decimal SignupPrice(string? package) =>
        CanSelectSignupPackage(package) ? PremiumPrice : 0m;

    public static MembershipPayment? CreateSignupPayment(User member, DateTime paidAtUtc)
    {
        var amount = SignupPrice(member.Package);
        return amount > 0 ? new MembershipPayment
        {
            Member = member,
            MemberName = member.Name,
            Package = PremiumPackage,
            Amount = amount,
            PaidAt = paidAtUtc
        } : null;
    }

    public static bool CanViewRevenue(int? accountId, string? role) =>
        accountId is > 0 && role?.ToUpperInvariant() is "ACCOUNTING" or "ADMIN" or "OWNER";

    public static (DateTime Start, DateTime End) BangkokMonthUtc(DateTime utcNow)
    {
        var bangkok = FoodPricing.BangkokTime(utcNow);
        var start = new DateTime(bangkok.Year, bangkok.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start.AddHours(-7), start.AddMonths(1).AddHours(-7));
    }
}
