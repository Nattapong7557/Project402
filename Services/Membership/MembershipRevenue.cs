using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.Membership;
using Nattapong_7379_L002.ViewModel.Membership;

namespace Nattapong_7379_L002.Services.Membership;

public sealed class MembershipRevenue(Csi402dbContext db, TimeProvider clock)
{
    private const int PageSize = 25;

    public Task<decimal> GetMonthlyTotalAsync(DateTime start, DateTime end) => db.MembershipPayments
        .Where(payment => payment.PaidAt >= start && payment.PaidAt < end)
        .SumAsync(payment => payment.Amount);

    public async Task<MembershipRevenueViewModel> ReadAsync(int? page = null)
    {
        var (start, end) = MembershipRules.BangkokMonthUtc(clock.GetUtcNow().UtcDateTime);
        var totals = await db.MembershipPayments.GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Sum(payment => payment.Amount),
                Monthly = group.Sum(payment => payment.PaidAt >= start && payment.PaidAt < end ? payment.Amount : 0m),
                Count = group.Count()
            }).SingleOrDefaultAsync();

        var model = new MembershipRevenueViewModel
        {
            Total = totals?.Total ?? 0m,
            MonthlyTotal = totals?.Monthly ?? 0m,
            PageCount = Math.Max(1, (int)Math.Ceiling((totals?.Count ?? 0) / (decimal)PageSize))
        };
        if (page is null)
        {
            model.Payments = await LatestPayments().Take(5).ToListAsync();
        }
        else
        {
            model.Page = Math.Clamp(page.Value, 1, model.PageCount);
            model.Payments = await LatestPayments().Skip((model.Page - 1) * PageSize).Take(PageSize).ToListAsync();
        }

        return model;
    }

    private IOrderedQueryable<MembershipPayment> LatestPayments() => db.MembershipPayments.AsNoTracking()
        .OrderByDescending(payment => payment.PaidAt).ThenByDescending(payment => payment.Id);
}
