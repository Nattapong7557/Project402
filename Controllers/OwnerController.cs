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

public class OwnerController : Controller
{
    private readonly Csi402dbContext _db;

    private readonly MonthlyProfit _profit;

    public OwnerController(Csi402dbContext db, MonthlyProfit profit)
    {
        _db = db;
        _profit = profit;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("AccountRole");
        if (string.IsNullOrEmpty(role) ||
            (!string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AccessDenied"] = "หน้านี้สำหรับ Owner หรือ Admin เท่านั้น";
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bookings = _db.Bookings.AsNoTracking().Where(BookingQueue.AppliesTo);

        var usersCount = await _db.Users.CountAsync();
        var servicesCount = await _db.ServiceItems.CountAsync();
        var model = new OwnerDashboardViewModel
        {
            Profit = await _profit.ReadAsync(),
            TotalBookingsCount = await bookings.CountAsync(),
            TotalUsersCount = usersCount,
            TotalServicesCount = servicesCount,
            RecentBookings = await bookings.OrderByDescending(booking => booking.Id).Take(5).ToListAsync()
        };

        return View(model);
    }
}
