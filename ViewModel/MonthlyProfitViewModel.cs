namespace Nattapong_7379_L002.ViewModel;

public sealed record MonthlyProfitViewModel(
    DateOnly Month,
    decimal FoodRevenue,
    decimal MembershipRevenue,
    decimal ManualIncome,
    decimal Expenses,
    int UnreadableRecords)
{
    public decimal TotalIncome => FoodRevenue + MembershipRevenue + ManualIncome;
    public decimal NetProfit => TotalIncome - Expenses;
}
