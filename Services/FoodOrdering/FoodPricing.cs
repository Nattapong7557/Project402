using System.Globalization;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.FoodOrdering;

namespace Nattapong_7379_L002.Services.FoodOrdering;

public static class FoodPricing
{
    public const decimal BirthdayMinimum = 1500m;
    public const string Birthday = "Birthday";
    public const string ComboName = "All-in-One Studio & Café Combo";

    public static DateTime BangkokTime(DateTime utcTime) => utcTime.AddHours(7);

    public static bool IsDrink(string category) => category is "coffee" or "tea" or "beverage";

    public static bool TryBirthDate(string value, DateOnly today, out DateOnly birthDate) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate) &&
        birthDate >= new DateOnly(1900, 1, 1) && birthDate <= today;

    public static (string? Code, decimal Amount) Discount(decimal subtotal, int drinks, DateTime localTime, bool birthdayEligible)
    {
        var candidates = new (string? Code, decimal Amount)[]
        {
            (Birthday, birthdayEligible && subtotal >= BirthdayMinimum ? BirthdayAmount(subtotal) : 0m),
            ("Order1000", subtotal >= 1000 ? 100m : 0m),
            ("FiveDrinks", drinks >= 5 ? 50m : 0m),
            ("Afternoon", localTime.Hour is >= 13 and < 16 ? 50m : 0m)
        };
        var best = candidates.MaxBy(candidate => candidate.Amount);
        return best.Amount > 0 ? (best.Code, Math.Min(subtotal, best.Amount)) : (null, 0m);
    }

    private static decimal BirthdayAmount(decimal subtotal) =>
        decimal.Round(subtotal * .15m, 2, MidpointRounding.AwayFromZero);

    public static decimal? BirthdayAtBilling(FoodOrder order, DateTime localTime, DateOnly? birthDate, bool alreadyUsed)
    {
        if (order.DiscountCode == Birthday || order.Subtotal < BirthdayMinimum ||
            alreadyUsed || birthDate?.Month != localTime.Month)
            return null;

        var amount = BirthdayAmount(order.Subtotal);
        return amount > order.Discount ? amount : null;
    }

    public static string DiscountLabel(string? code) => code switch
    {
        Birthday => "ส่วนลดเดือนเกิด",
        "Order1000" => "ส่วนลดเมื่อสั่งครบ ฿1,000",
        "FiveDrinks" => "ส่วนลดเครื่องดื่ม 5 แก้ว",
        "Afternoon" => "ส่วนลดช่วง 13:00–16:00 น.",
        _ => "ส่วนลด"
    };

    public static decimal ComboDiscount(ServiceItem item) =>
        item.ServiceType == "Promotion" && item.Name == ComboName ? Math.Min(300m, item.Price) : 0m;
}
