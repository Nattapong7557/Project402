using Microsoft.AspNetCore.Mvc;
using Nattapong_7379_L002.Services.Membership;

namespace Nattapong_7379_L002.Controllers;

public sealed class MembershipRevenueController(MembershipRevenue revenue) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var accountId = HttpContext.Session.GetInt32("AccountId");
        if (accountId is not > 0) return RedirectToAction("Login", "Account");

        if (!MembershipRules.CanViewRevenue(accountId, HttpContext.Session.GetString("AccountRole")))
            return StatusCode(StatusCodes.Status403Forbidden);

        return View(await revenue.ReadAsync(page));
    }
}
