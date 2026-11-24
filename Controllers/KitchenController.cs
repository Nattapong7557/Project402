using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Services;
using Nattapong_7379_L002.ViewModel;

namespace Nattapong_7379_L002.Controllers;

public class KitchenController : Controller
{
    private readonly Csi402dbContext _db;

    public KitchenController(Csi402dbContext db)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("AccountRole");
        if (string.IsNullOrEmpty(role) ||
            (!string.Equals(role, "Kitchen staff", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AccessDenied"] = "หน้านี้สำหรับเจ้าหน้าที่ Kitchen staff หรือ Admin เท่านั้น";
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Kitchen", "FoodStaff");

    [HttpGet]
    public async Task<IActionResult> Bookings()
    {
        var orders = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.User)
            .Where(TableBookingWorkflow.ForKitchen)
            .OrderByDescending(b => b.Id)
            .ToListAsync();

        var model = new KitchenViewModel
        {
            KitchenOrders = orders,
            PreparingCount = orders.Count(order => TableBookingWorkflow.NextForKitchen(order) is not null),
            ReadyCount = orders.Count(order => order.Status == "Served")
        };

        return View(nameof(Index), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(KitchenViewModel model)
    {
        var order = await _db.Bookings.AsNoTracking().SingleOrDefaultAsync(booking => booking.Id == model.SelectedOrderId);
        if (order is not null && TableBookingWorkflow.NextForKitchen(order) is { } next && next == model.NewStatus)
        {
            var changed = await _db.Bookings
                .Where(booking => booking.Id == order.Id && booking.ServiceType == order.ServiceType && booking.Status == order.Status)
                .ExecuteUpdateAsync(update => update.SetProperty(booking => booking.Status, next));
            if (changed == 1)
            {
                TempData["AdminSuccess"] = $"อัปเดตสถานะโต๊ะ #{order.Id} เรียบร้อยแล้ว";
                return RedirectToAction(nameof(Bookings));
            }
        }

        TempData["AdminError"] = "อัปเดตสถานะไม่ได้ กรุณาตรวจสอบสถานะล่าสุดของรายการ";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddKitchenOrder(KitchenViewModel model)
    {
        var accountId = HttpContext.Session.GetInt32("AccountId") ?? 1;
        if (!string.IsNullOrWhiteSpace(model.TableNumber) && !string.IsNullOrWhiteSpace(model.FoodItems))
        {
            var newOrder = new Booking
            {
                UserId = accountId,
                ServiceType = "Kitchen",
                ItemName = model.TableNumber.Trim(),
                BookingDate = DateTime.Today.ToString("yyyy-MM-dd"),
                TimeSlot = DateTime.Now.ToString("HH:mm") + " น.",
                GuestCount = 1,
                Note = model.FoodItems.Trim(),
                Status = "Preparing",
                CreatedAt = DateTime.UtcNow
            };
            _db.Bookings.Add(newOrder);
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"เพิ่มรายการอาหารสำหรับ '{newOrder.ItemName}' เข้าครัวเรียบร้อยแล้ว";
        }
        return RedirectToAction(nameof(Bookings));
    }
}
