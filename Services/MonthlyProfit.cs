using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Services.FoodOrdering;
using Nattapong_7379_L002.Services.Membership;
using Nattapong_7379_L002.ViewModel;

namespace Nattapong_7379_L002.Services;

public sealed class MonthlyProfit(Csi402dbContext db, MembershipRevenue membership, TimeProvider clock)
{
    public async Task<MonthlyProfitViewModel> ReadAsync()
    {
        var (start, end) = MembershipRules.BangkokMonthUtc(clock.GetUtcNow().UtcDateTime);
        var foodRevenue = await db.FoodPayments
            .Where(payment => payment.PaidAt >= start && payment.PaidAt < end)
            .SumAsync(payment => payment.Amount);
        var membershipRevenue = await membership.GetMonthlyTotalAsync(start, end);
        var manualRecords = await db.Bookings.AsNoTracking()
            .Where(AccountingRules.ManualRecords)
            .Where(record => record.CreatedAt >= start && record.CreatedAt < end)
            .ToListAsync();

        return AccountingRules.Summarize(
            DateOnly.FromDateTime(FoodPricing.BangkokTime(start)), foodRevenue, membershipRevenue, manualRecords);
    }
}
