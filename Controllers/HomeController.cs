using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Services.FoodOrdering;

namespace Nattapong_7379_L002.Controllers;

public class HomeController(Csi402dbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var workspace = StaffWorkspace.ForRole(HttpContext.Session.GetString("AccountRole"));
        if (workspace is not null) return RedirectToAction(workspace.Action, workspace.Controller);

        return View(await db.FoodMenuItems.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Id).ToListAsync());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
