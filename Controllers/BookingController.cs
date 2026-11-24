using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Models.FoodOrdering;
using Nattapong_7379_L002.Services.FoodOrdering;
using Nattapong_7379_L002.Services;
using Nattapong_7379_L002.ViewModel;

namespace Nattapong_7379_L002.Controllers;

public class BookingController : Controller
{
    private readonly Csi402dbContext _db;

    private readonly TableReservations _tables;

    public BookingController(Csi402dbContext db, TableReservations tables)
    {
        _db = db;
        _tables = tables;
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        var workspace = StaffWorkspace.ForRole(HttpContext.Session.GetString("AccountRole"));

        if (workspace is not null)
        {
            context.Result = context.ActionDescriptor.RouteValues["action"] == nameof(StatusUpdates)
                ? StatusCode(StatusCodes.Status403Forbidden)
                : RedirectToAction(workspace.Action, workspace.Controller);
            return;
        }

        base.OnActionExecuting(context);
    }

    // Helper: ตรวจสอบสถานะการเข้าสู่ระบบ
    private bool IsUserLoggedIn(out int userId)
    {
        userId = HttpContext.Session.GetInt32("AccountId") ?? 0;
        return userId > 0;
    }

    // 1. My Bookings (ประวัติการจองของฉัน)
    public async Task<IActionResult> Index()
    {
        if (!IsUserLoggedIn(out var userId))
        {
            TempData["AccessDenied"] = "กรุณาเข้าสู่ระบบเพื่อดูประวัติและจัดการการจองของคุณ";
            return RedirectToAction("Login", "Account");
        }

        ViewData["Title"] = "ประวัติการจองของฉัน";
        var bookings = await _db.Bookings
            .Where(b => b.UserId == userId)
            .Include(booking => booking.Charge)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> StatusUpdates(CancellationToken cancellationToken)
    {
        if (!IsUserLoggedIn(out var userId))
            return Unauthorized();

        var bookings = await _db.Bookings.AsNoTracking()
            .Where(booking => booking.UserId == userId)
            .Select(booking => new { booking.Id, booking.Status })
            .ToListAsync(cancellationToken);

        return Json(bookings.Select(booking => new
        {
            booking.Id,
            Status = CustomerBookingStatus.Describe(booking.Status)
        }));
    }

    // 2. Menu-Promotion (โปรโมชั่นพิเศษสำหรับสมาชิก)
    [HttpGet]
    public async Task<IActionResult> Promotions()
    {
        ViewData["Title"] = "โปรโมชั่นสมาชิก VIP";
        var items = await _db.ServiceItems
            .Where(s => s.ServiceType == "Promotion" && s.IsActive)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();
        return View(items);
    }

    // 3. Menu-ช่างแต่งหน้า (จองคิวช่างแต่งหน้า)
    [HttpGet]
    public async Task<IActionResult> Makeup()
    {
        ViewData["Title"] = "บริการช่างแต่งหน้า";
        var items = await _db.ServiceItems
            .Where(s => s.ServiceType == "Makeup" && s.IsActive)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();
        return View(items);
    }

    // 4. Menu-ช่างภาพ (จองคิวช่างภาพ)
    [HttpGet]
    public async Task<IActionResult> Photographer()
    {
        ViewData["Title"] = "บริการช่างภาพสตูดิโอ";
        var items = await _db.ServiceItems
            .Where(s => s.ServiceType == "Photographer" && s.IsActive)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();
        return View(items);
    }

    // 5. Menu-เช่าชุด (เช่าชุดคอสตูม & สตูดิโอ)
    [HttpGet]
    public async Task<IActionResult> Costumes()
    {
        ViewData["Title"] = "บริการเช่าชุดสตูดิโอ";
        var items = await _db.ServiceItems
            .Where(s => s.ServiceType == "Costume" && s.IsActive)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();
        return View(items);
    }

    // 6. Menu-จอง (จองโต๊ะ & ที่นั่งผัง 3D Isometric ตาม Figma)
    [HttpGet]
    public async Task<IActionResult> Table()
    {
        var tomorrow = DateOnly.FromDateTime(_tables.LocalNow).AddDays(1);
        return await RenderTable(new BookingFormViewModel
        {
            ItemName = TableReservationRules.Zones[0].BookingName,
            BookingDate = TableReservationRules.FormatDate(tomorrow),
            TimeSlot = TableReservationRules.Slots[2].Value,
            GuestCount = 2
        });
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TableAvailability(string? zone, string? date)
    {
        var seatingZone = TableReservationRules.FindZone(zone);
        if (seatingZone is null || !TableReservationRules.TryDate(date, out var visitDate))
            return BadRequest();

        return Json(new { unavailable = await _tables.UnavailableAsync(seatingZone, visitDate) });
    }

    private async Task<IActionResult> RenderTable(BookingFormViewModel model, string? error = null)
    {
        var zone = TableReservationRules.FindZone(model.ItemName) ?? TableReservationRules.Zones[0];
        var today = DateOnly.FromDateTime(_tables.LocalNow);
        var validDate = TableReservationRules.TryDate(model.BookingDate, out var date) && date >= today;
        if (!validDate) date = today.AddDays(1);
        var unavailable = await _tables.UnavailableAsync(zone, date);
        ModelState.Clear();
        ViewData["TableError"] = error;
        return View(nameof(Table), new TableBookingViewModel
        {
            ItemName = zone.BookingName,
            BookingDate = TableReservationRules.FormatDate(date),
            MinimumDate = TableReservationRules.FormatDate(today),
            TimeSlot = validDate && !unavailable.Contains(model.TimeSlot) ? model.TimeSlot : string.Empty,
            GuestCount = model.GuestCount,
            Note = model.Note,
            Unavailable = unavailable
        });
    }

    private async Task<IActionResult> BookTable(BookingFormViewModel model, int userId)
    {
        var zone = TableReservationRules.FindZone(model.ItemName);
        var slot = TableReservationRules.FindSlot(model.TimeSlot);
        if (!ModelState.IsValid || zone is null || slot is null || !TableReservationRules.TryDate(model.BookingDate, out var date) ||
            TableReservationRules.HasStarted(date, slot.Start, _tables.LocalNow) || model.GuestCount is < 1 or > 15)
            return await RenderTable(model, "กรุณาเลือกโซน วันที่และรอบที่ยังไม่เริ่ม พร้อมระบุจำนวนผู้เข้าร่วม 1–15 ท่าน");

        var booking = new Booking
        {
            UserId = userId,
            ServiceType = "Table",
            ItemName = zone.BookingName,
            BookingDate = TableReservationRules.FormatDate(date),
            TimeSlot = slot.Value,
            GuestCount = model.GuestCount,
            Note = model.Note?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        var result = await _tables.TrySaveAsync(booking, zone, date);
        if (result != ZoneReservationResult.Saved)
            return await RenderTable(model, result switch
            {
                ZoneReservationResult.Occupied => "โซนนี้มีรายการจองทับรอบที่เลือกแล้ว กรุณาเลือกรอบหรือโซนอื่น",
                ZoneReservationResult.Started => "รอบนี้เริ่มแล้ว กรุณาเลือกรอบอื่น",
                _ => "ขณะนี้มีการจองพร้อมกัน กรุณาลองอีกครั้ง"
            });

        return BookingSaved(booking);
    }

    // Action บันทึกการจอง (ใช้ร่วมกันทุกบริการ)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookingFormViewModel model)
    {
        if (!IsUserLoggedIn(out var userId))
        {
            TempData["AccessDenied"] = "กรุณาเข้าสู่ระบบก่อนทำการจองบริการ";
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(model.ServiceType) || string.Equals(model.ServiceType?.Trim(), "Table", StringComparison.OrdinalIgnoreCase))
            return await BookTable(model, userId);

        if (string.IsNullOrWhiteSpace(model.ItemName) || string.IsNullOrWhiteSpace(model.BookingDate))
        {
            TempData["BookingError"] = "กรุณากรอกข้อมูลวันที่และรายการที่ต้องการจองให้ครบถ้วน";
            return RedirectToAction(model.ServiceType);
        }

        var booking = new Booking
        {
            UserId = userId,
            ServiceType = model.ServiceType ?? "Table",
            ItemName = model.ItemName.Trim(),
            BookingDate = model.BookingDate.Trim(),
            TimeSlot = model.TimeSlot?.Trim() ?? "12:00 - 14:00",
            GuestCount = model.GuestCount > 0 ? model.GuestCount : 1,
            Note = model.Note?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        if (booking.ServiceType == "Promotion" &&
            (model.ServiceItemId.HasValue || booking.ItemName == FoodPricing.ComboName))
        {
            var activePromotions = _db.ServiceItems.AsNoTracking().Where(item => item.ServiceType == "Promotion" && item.IsActive);
            var promotion = model.ServiceItemId is int itemId
                ? await activePromotions.FirstOrDefaultAsync(item => item.Id == itemId)
                : await activePromotions.FirstOrDefaultAsync(item => item.Name == booking.ItemName);
            if (promotion is null)
            {
                TempData["ErrorMessage"] = "โปรโมชันนี้ไม่พร้อมให้จอง กรุณาเลือกใหม่";
                return RedirectToAction(nameof(Promotions));
            }

            booking.ItemName = promotion.Name;
            var discount = FoodPricing.ComboDiscount(promotion);
            if (discount > 0)
                booking.Charge = new BookingCharge
                {
                    ServiceItemId = promotion.Id, Subtotal = promotion.Price, Discount = discount,
                    Total = promotion.Price - discount
                };
        }

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        return BookingSaved(booking);
    }

    private IActionResult BookingSaved(Booking booking)
    {
        TempData["BookingSuccess"] = $"ส่งคำขอจอง {booking.ItemName} ในวันที่ {booking.BookingDate} ({booking.TimeSlot}) เรียบร้อยแล้ว! ขณะนี้อยู่ระหว่างรอ Staff Queue อนุมัติรับคิวครับ";
        return RedirectToAction(nameof(Index));
    }

    // ยกเลิกการจอง
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!IsUserLoggedIn(out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var changed = await _db.Bookings.Where(CustomerBookingStatus.Cancellable)
            .Where(booking => booking.Id == id && booking.UserId == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(booking => booking.Status, "Cancelled"));
        if (changed == 1)
            TempData["BookingSuccess"] = "ยกเลิกการจองเรียบร้อยแล้ว";
        else
            TempData["BookingError"] = "ยกเลิกรายการนี้ไม่ได้ กรุณาตรวจสอบสถานะล่าสุด";

        return RedirectToAction(nameof(Index));
    }
}

