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

public class StaffQueueController : Controller
{
    private readonly Csi402dbContext _db;

    private readonly TableReservations _tables;

    public StaffQueueController(Csi402dbContext db, TableReservations tables)
    {
        _db = db;
        _tables = tables;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("AccountRole");
        if (string.IsNullOrEmpty(role) ||
            (!string.Equals(role, "Staff queue", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AccessDenied"] = "หน้านี้สำหรับเจ้าหน้าที่ Staff queue หรือ Admin เท่านั้น";
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var queueItems = await _db.Bookings
            .AsNoTracking().Where(BookingQueue.AppliesTo)
            .Include(b => b.User)
            .OrderByDescending(b => b.Id)
            .ToListAsync();

        var model = new StaffQueueViewModel
        {
            QueueItems = queueItems
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQueueStatus(StaffQueueViewModel model)
    {
        if (BookingQueue.IsQueueDecision(model.NewStatus))
        {
            var changed = await _db.Bookings.Where(BookingQueue.AppliesTo)
                .Where(booking => booking.Id == model.SelectedBookingId && booking.Status == "Pending")
                .ExecuteUpdateAsync(update => update.SetProperty(booking => booking.Status, model.NewStatus));
            if (changed == 1)
                TempData["AdminSuccess"] = model.NewStatus == "QueueConfirmed" ? "อนุมัติรับคิวแล้ว" : "ยกเลิกคำขอจองแล้ว";
            else
                TempData["AdminError"] = "รายการนี้ไม่มีอยู่ในคิวรออนุมัติแล้ว";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWalkInQueue(StaffQueueViewModel model)
    {
        var accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
        if (accountId > 0 && BookingQueue.AllowsWalkIn(model.WalkInServiceType) &&
            !string.IsNullOrWhiteSpace(model.WalkInItemName) && model.WalkInItemName.Trim().Length <= 150 &&
            !string.IsNullOrWhiteSpace(model.WalkInTimeSlot) && model.WalkInTimeSlot.Trim().Length <= 50 &&
            model.WalkInGuestCount is >= 1 and <= 15)
        {
            var booking = new Booking
            {
                UserId = accountId,
                ServiceType = model.WalkInServiceType,
                ItemName = model.WalkInItemName.Trim(),
                BookingDate = DateTime.Today.ToString("yyyy-MM-dd"),
                TimeSlot = model.WalkInTimeSlot.Trim(),
                GuestCount = model.WalkInGuestCount,
                Note = model.WalkInNote?.Trim(),
                Status = "QueueConfirmed",
                CreatedAt = DateTime.UtcNow
            };
            if (booking.ServiceType == "Table" && TableReservationRules.FindZone(booking.ItemName) is { } zone)
            {
                if (!TableReservationRules.TryTimeRange(booking.TimeSlot, out _, out _))
                {
                    TempData["AdminError"] = "กรุณาระบุช่วงเวลา เช่น 13:00 - 15:00 เมื่อออกคิวตามโซน";
                    return RedirectToAction(nameof(Index));
                }

                var date = DateOnly.FromDateTime(_tables.LocalNow);
                booking.BookingDate = TableReservationRules.FormatDate(date);
                booking.ItemName = zone.BookingName;
                var result = await _tables.TrySaveAsync(booking, zone, date);
                if (result != ZoneReservationResult.Saved)
                {
                    TempData["AdminError"] = result == ZoneReservationResult.Occupied
                        ? "โซนนี้มีรายการทับช่วงเวลาที่เลือก"
                        : "ขณะนี้มีการจองพร้อมกัน กรุณาลองอีกครั้ง";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();
            }
            TempData["AdminSuccess"] = $"ออกบัตรคิว Walk-in '{booking.ItemName}' เรียบร้อยแล้ว";
        }
        else
            TempData["AdminError"] = "กรุณาระบุประเภท รายการ รอบเวลา และจำนวนผู้เข้าร่วมให้ถูกต้อง";
        return RedirectToAction(nameof(Index));
    }
}
