using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.FoodOrdering;
using Nattapong_7379_L002.Services;
using Nattapong_7379_L002.Services.FoodOrdering;
using Nattapong_7379_L002.ViewModel.FoodOrdering;
using Nattapong_7379_L002.Services.Membership;
using Nattapong_7379_L002.ViewModel;
using Nattapong_7379_L002.ViewModel.Membership;

namespace Nattapong_7379_L002.Controllers;

public sealed class FoodStaffController(
    Csi402dbContext db, FoodOrderService orders, MembershipRevenue membershipRevenue, MonthlyProfit monthlyProfit) : Controller
{
    private int AccountId => HttpContext.Session.GetInt32("AccountId") ?? 0;
    private string? Role => HttpContext.Session.GetString("AccountRole");

    [HttpGet]
    public Task<IActionResult> Cashier() => Queue(nameof(Cashier), "รับออเดอร์และเสิร์ฟอาหาร", FoodOrderRules.Cashier,
        FoodOrderStatus.Submitted, FoodOrderStatus.Ready);

    [HttpGet]
    public Task<IActionResult> Kitchen() => Queue(nameof(Kitchen), "ออเดอร์ในครัว", FoodOrderRules.Kitchen, FoodOrderStatus.InKitchen);

    [HttpGet]
    public Task<IActionResult> Accounting() => Queue(nameof(Accounting), "รับชำระบิลอาหาร", FoodOrderRules.Accounting,
        FoodOrderStatus.AwaitingPayment, FoodOrderStatus.Paid);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Accept(int id) => Advance(id, FoodOrderAction.Accept, nameof(Cashier));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> MarkReady(int id) => Advance(id, FoodOrderAction.MarkReady, nameof(Kitchen));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Serve(int id) => Advance(id, FoodOrderAction.Serve, nameof(Cashier));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Pay(int id) => Advance(id, FoodOrderAction.Pay, nameof(Accounting));

    private async Task<IActionResult> Queue(string workspace, string title, string role, params FoodOrderStatus[] statuses)
    {
        if (!CanAccess(role)) return AccessDenied();
        var activeStatuses = statuses.Where(status => status != FoodOrderStatus.Paid).ToList();
        var active = await db.FoodOrders.AsNoTracking().Where(order => activeStatuses.Contains(order.Status))
            .Include(order => order.Items).OrderBy(order => order.Id).AsSplitQuery().ToListAsync();
        var paidRevenue = 0m;
        MembershipRevenueViewModel? membership = null;
        MonthlyProfitViewModel? profit = null;
        if (workspace == nameof(Accounting))
        {
            paidRevenue = await db.FoodPayments.SumAsync(payment => payment.Amount);
            profit = await monthlyProfit.ReadAsync();
            membership = await membershipRevenue.ReadAsync();
            var recentPayments = await db.FoodOrders.AsNoTracking().Where(order => order.Status == FoodOrderStatus.Paid)
                .Include(order => order.Items).Include(order => order.Payment)
                .OrderByDescending(order => order.Payment!.PaidAt).ThenByDescending(order => order.Id)
                .Take(20).AsSplitQuery().ToListAsync();
            active.AddRange(recentPayments);
        }

        return View("Index", new FoodStaffViewModel
        {
            Title = title, Workspace = workspace, Orders = active, PaidRevenue = paidRevenue, Membership = membership,
            Profit = profit
        });
    }

    private async Task<IActionResult> Advance(int id, FoodOrderAction action, string workspace)
    {
        if (!CanAccess(FoodOrderRules.Transition(action).Role)) return AccessDenied();
        var changed = await orders.AdvanceAsync(id, action, AccountId, Role);
        TempData[changed ? "FoodSuccess" : "FoodError"] = changed
            ? $"อัปเดตออเดอร์ #{id} เรียบร้อยแล้ว"
            : "สถานะออเดอร์เปลี่ยนไปแล้ว กรุณาตรวจสอบรายการล่าสุด";
        return RedirectToAction(workspace);
    }

    private bool CanAccess(string role) => AccountId > 0 && FoodOrderRules.IsStaff(Role, role);
    private IActionResult AccessDenied() => AccountId == 0 ? RedirectToAction("Login", "Account") : StatusCode(StatusCodes.Status403Forbidden);
}
