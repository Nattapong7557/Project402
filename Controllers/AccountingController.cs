using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Services;
using Nattapong_7379_L002.Services.FoodOrdering;
using Nattapong_7379_L002.ViewModel;

namespace Nattapong_7379_L002.Controllers;

public class AccountingController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly MonthlyProfit _profit;
    private readonly TimeProvider _clock;

    public AccountingController(Csi402dbContext db, MonthlyProfit profit, TimeProvider clock)
    {
        _db = db;
        _profit = profit;
        _clock = clock;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("AccountRole");
        if (string.IsNullOrEmpty(role) ||
            (!string.Equals(role, "Accounting", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AccessDenied"] = "หน้านี้สำหรับเจ้าหน้าที่ Accounting หรือ Admin เท่านั้น";
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Accounting", "FoodStaff");

    [HttpGet]
    public async Task<IActionResult> Records()
    {
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(AccountingRules.ManualRecords)
            .OrderByDescending(b => b.Id)
            .ToListAsync();

        var model = new AccountingViewModel
        {
            Transactions = bookings,
            Profit = await _profit.ReadAsync()
        };

        return View(nameof(Index), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRecord(AccountingViewModel model)
    {
        var accountId = HttpContext.Session.GetInt32("AccountId");
        if (accountId is > 0 && !string.IsNullOrWhiteSpace(model.Title) && model.Title.Trim().Length <= 100 &&
            model.RecordType is "Income" or "Expense" && AccountingRules.IsValidAmount(model.Amount))
        {
            var utcNow = _clock.GetUtcNow().UtcDateTime;
            var bangkok = FoodPricing.BangkokTime(utcNow);
            var record = new Booking
            {
                UserId = accountId.Value,
                ServiceType = model.RecordType,
                ItemName = AccountingRules.FormatRecordName(model.Title, model.Amount),
                BookingDate = bangkok.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                TimeSlot = bangkok.ToString("HH:mm", CultureInfo.InvariantCulture) + " น.",
                GuestCount = 1,
                Note = model.Note?.Trim(),
                Status = model.RecordType == "Expense" ? "Expense" : "Paid",
                CreatedAt = utcNow
            };
            _db.Bookings.Add(record);
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"บันทึกรายการบัญชี '{model.Title}' จำนวน ฿{model.Amount:N2} สำเร็จ";
        }
        else
            TempData["AdminError"] = "กรุณาระบุประเภท ชื่อ และจำนวนเงินที่ถูกต้อง";
        return RedirectToAction(nameof(Records));
    }
}
