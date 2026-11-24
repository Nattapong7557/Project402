using System.Globalization;
using System.Linq.Expressions;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.ViewModel;

namespace Nattapong_7379_L002.Services;

public static class AccountingRules
{
    public static Expression<Func<Booking, bool>> ManualRecords => record =>
        record.ServiceType == "Income" || record.ServiceType == "Expense";

    public static bool IsValidAmount(decimal amount) =>
        amount is >= 0.01m and <= 10_000_000m && decimal.Round(amount, 2) == amount;

    public static string FormatRecordName(string title, decimal amount) =>
        string.Create(CultureInfo.InvariantCulture, $"{title.Trim()} (฿{amount:N2})");

    public static MonthlyProfitViewModel Summarize(
        DateOnly month, decimal foodRevenue, decimal membershipRevenue, IEnumerable<Booking> manualRecords)
    {
        var income = 0m;
        var expenses = 0m;
        var unreadableRecords = 0;
        foreach (var record in manualRecords)
        {
            if (!TryReadAmount(record.ItemName, out var amount))
                unreadableRecords++;
            else if (record.ServiceType == "Income")
                income += amount;
            else
                expenses += amount;
        }

        return new MonthlyProfitViewModel(month, foodRevenue, membershipRevenue, income, expenses, unreadableRecords);
    }

    private static bool TryReadAmount(string itemName, out decimal amount)
    {
        const string marker = " (฿";
        amount = 0m;
        var start = itemName.LastIndexOf(marker, StringComparison.Ordinal);
        if (start < 0 || !itemName.EndsWith(')'))
            return false;

        var text = itemName.AsSpan(start + marker.Length, itemName.Length - start - marker.Length - 1);
        return TryReadFormattedAmount(text, CultureInfo.InvariantCulture, out amount) ||
               TryReadFormattedAmount(text, CultureInfo.CurrentCulture, out amount);
    }

    private static bool TryReadFormattedAmount(ReadOnlySpan<char> text, CultureInfo culture, out decimal amount)
    {
        const NumberStyles format = NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands;
        return decimal.TryParse(text, format, culture, out amount) &&
               IsValidAmount(amount) &&
               (text.SequenceEqual(amount.ToString("N2", culture)) || text.SequenceEqual(amount.ToString("F2", culture)));
    }
}
