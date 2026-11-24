using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.FoodOrdering;
using Nattapong_7379_L002.Services.FoodOrdering;
using Nattapong_7379_L002.ViewModel.FoodOrdering;

namespace Nattapong_7379_L002.Controllers;

public sealed class FoodOrderController(Csi402dbContext db, FoodOrderService orders) : Controller
{
    private int AccountId => HttpContext.Session.GetInt32("AccountId") ?? 0;
    private string? Role => HttpContext.Session.GetString("AccountRole");
    private bool CanOrder => AccountId > 0 && FoodOrderRules.CanOrder(Role);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var menu = await db.FoodMenuItems.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Id).ToListAsync();
        return View(new FoodMenuViewModel
        {
            Menu = menu,
            CanOrder = CanOrder,
            Order = new FoodOrderInput
            {
                RequestToken = Guid.NewGuid().ToString("N"),
                Lines = menu.Select(item => new FoodOrderLineInput { MenuItemId = item.Id }).ToList()
            }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(FoodOrderInput order)
    {
        if (!CanOrder) return SignIn(nameof(Index));
        if (!ModelState.IsValid)
        {
            TempData["FoodError"] = "ตรวจสอบหมายเลขโต๊ะและจำนวนที่เลือก (สูงสุด 20 ต่อเมนู)";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var id = await orders.SubmitAsync(AccountId, order);
            TempData["FoodSuccess"] = $"ส่งออเดอร์ #{id} แล้ว กรุณารอแคชเชียร์ยืนยัน";
            return RedirectToAction(nameof(Orders));
        }
        catch (FoodOrderException error)
        {
            TempData["FoodError"] = error.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public Task<IActionResult> Orders() => OrderList(bills: false);

    [HttpGet]
    public Task<IActionResult> Bills() => OrderList(bills: true);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestBill(int id, string? birthDate)
    {
        if (!CanOrder) return SignIn(nameof(Bills));
        try
        {
            var changed = await orders.RequestBillAsync(id, AccountId, Role, birthDate);
            TempData[changed ? "FoodSuccess" : "FoodError"] = changed
                ? "ส่งคำขอเช็คบิลแล้ว ตรวจยอดสุทธิด้านล่างและรอเจ้าหน้าที่บัญชียืนยัน"
                : "รายการนี้ยังขอเช็คบิลไม่ได้ หรือได้ส่งคำขอแล้ว";
        }
        catch (FoodOrderException error)
        {
            TempData["FoodError"] = error.Message;
        }
        return RedirectToAction(nameof(Bills));
    }

    private async Task<IActionResult> OrderList(bool bills)
    {
        if (!CanOrder) return SignIn(bills ? nameof(Bills) : nameof(Orders));

        var query = db.FoodOrders.AsNoTracking().Where(order => order.UserId == AccountId);
        if (bills)
            query = query.Where(order => order.Status == FoodOrderStatus.Served ||
                order.Status == FoodOrderStatus.AwaitingPayment || order.Status == FoodOrderStatus.Paid);

        return View("Orders", new FoodOrderListViewModel
        {
            Bills = bills,
            NeedsBirthDate = bills && !await db.FoodCustomerProfiles.AnyAsync(profile => profile.UserId == AccountId),
            Orders = await query.Include(order => order.Items).Include(order => order.Payment)
                .OrderByDescending(order => order.Id).AsSplitQuery().ToListAsync()
        });
    }

    private IActionResult SignIn(string returnAction) => AccountId == 0
        ? RedirectToAction("Login", "Account", new { returnUrl = Url.Action(returnAction, "FoodOrder") })
        : StatusCode(StatusCodes.Status403Forbidden);
}
