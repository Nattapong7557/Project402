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

public class CashierController : Controller
{
    private readonly Csi402dbContext _db;

    public CashierController(Csi402dbContext db)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("AccountRole");
        if (string.IsNullOrEmpty(role) ||
            (!string.Equals(role, "Cashiers and staff", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AccessDenied"] = "หน้านี้สำหรับเจ้าหน้าที่ Cashiers and staff หรือ Admin เท่านั้น";
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Cashier", "FoodStaff");

    [HttpGet]
    public async Task<IActionResult> Bookings()
    {
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.User)
            .Where(TableBookingWorkflow.ForCashier)
            .OrderByDescending(b => b.Id)
            .ToListAsync();

        var model = new CashierViewModel
        {
            Bookings = bookings,
            OpenTablesCount = bookings.Count(b => b.Status != "Completed" && b.Status != "Paid"),
            PaidTablesCount = bookings.Count(b => b.Status == "Paid" || b.Status == "Completed")
        };

        return View(nameof(Index), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(CashierViewModel model)
    {
        var booking = await _db.Bookings.AsNoTracking().SingleOrDefaultAsync(item => item.Id == model.SelectedBookingId);
        if (booking is not null && TableBookingWorkflow.NextForCashier(booking) is { } next && next == model.NewStatus)
        {
            var changed = await _db.Bookings
                .Where(item => item.Id == booking.Id && item.ServiceType == booking.ServiceType && item.Status == booking.Status)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, next));
            if (changed == 1)
            {
                TempData["AdminSuccess"] = $"อัปเดตสถานะโต๊ะ #{booking.Id} เรียบร้อยแล้ว";
                return RedirectToAction(nameof(Bookings));
            }
        }

        TempData["AdminError"] = "อัปเดตสถานะไม่ได้ กรุณาตรวจสอบสถานะล่าสุดของรายการ";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(CashierViewModel model)
    {
        var accountId = HttpContext.Session.GetInt32("AccountId") ?? 1;
        if (!string.IsNullOrWhiteSpace(model.NewTableItemName))
        {
            var newBooking = new Booking
            {
                UserId = accountId,
                ServiceType = "Table",
                ItemName = model.NewTableItemName.Trim(),
                BookingDate = DateTime.Today.ToString("yyyy-MM-dd"),
                TimeSlot = DateTime.Now.ToString("HH:mm") + " น.",
                GuestCount = model.NewTableGuestCount > 0 ? model.NewTableGuestCount : 1,
                Note = model.NewTableNote?.Trim(),
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow
            };
            _db.Bookings.Add(newBooking);
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"เปิดโต๊ะและรับออเดอร์ '{newBooking.ItemName}' เรียบร้อยแล้ว";
        }
        return RedirectToAction(nameof(Bookings));
    }
}
