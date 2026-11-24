using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.Services;
using Nattapong_7379_L002.ViewModel;

namespace Nattapong_7379_L002.Controllers;

public class AdminController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AdminController(Csi402dbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var accountId = HttpContext.Session.GetInt32("AccountId");
        if (accountId is null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var isAdmin = await _db.Staff
            .AsNoTracking()
            .AnyAsync(staff => staff.UserId == accountId.Value && staff.Role.Name == "Admin");

        if (!isAdmin)
        {
            TempData["AccessDenied"] = "ฟังก์ชันนี้สำหรับบัญชี Role Admin เท่านั้น";
            context.Result = new RedirectToActionResult("Index", "Home", null);
            return;
        }

        await next();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildUserListViewModelAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        var model = new AdminAddUserViewModel();
        await LoadRolesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AdminAddUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadRolesAsync(model);
            return View(model);
        }

        if (await _db.Users.AnyAsync(user => user.Email == model.Email.Trim()))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีบัญชีอยู่แล้ว");
            await LoadRolesAsync(model);
            return View(model);
        }

        if (await _db.Users.AnyAsync(user => user.Username == model.Username.Trim()))
        {
            ModelState.AddModelError(nameof(model.Username), "ชื่อผู้ใช้นี้ถูกใช้แล้ว");
            await LoadRolesAsync(model);
            return View(model);
        }

        var role = await _db.Roles.SingleOrDefaultAsync(item => item.Id == model.RoleId);
        if (role is null)
        {
            ModelState.AddModelError(nameof(model.RoleId), "Role ที่เลือกไม่มีอยู่ในระบบ");
            await LoadRolesAsync(model);
            return View(model);
        }

        var user = new User
        {
            Name = model.Name.Trim(),
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            PasswordHash = string.Empty
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        user.Staff = new Staff
        {
            User = user,
            Role = role,
            RoleId = role.Id,
            Status = "active"
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "บันทึกบัญชีไม่สำเร็จ กรุณาตรวจสอบข้อมูลอีกครั้ง");
            await LoadRolesAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminEditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "ข้อมูลที่แก้ไขไม่ถูกต้อง กรุณาตรวจสอบชื่อ อีเมล เบอร์โทร และ Role";
            return RedirectToAction(nameof(Index));
        }

        var user = await _db.Users
            .Include(account => account.Staff)
            .SingleOrDefaultAsync(account => account.Id == model.Id);
        if (user is null)
        {
            TempData["AdminError"] = "ไม่พบบัญชีผู้ใช้ที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Index));
        }

        var email = model.Email.Trim();
        var username = model.Username.Trim();
        if (await _db.Users.AnyAsync(account => account.Id != user.Id && account.Email == email))
        {
            TempData["AdminError"] = "อีเมลนี้ถูกใช้โดยบัญชีอื่นแล้ว";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Users.AnyAsync(account => account.Id != user.Id && account.Username == username))
        {
            TempData["AdminError"] = "ชื่อผู้ใช้นี้ถูกใช้โดยบัญชีอื่นแล้ว";
            return RedirectToAction(nameof(Index));
        }

        var role = await _db.Roles.SingleOrDefaultAsync(item => item.Id == model.RoleId);
        if (role is null)
        {
            TempData["AdminError"] = "Role ที่เลือกไม่มีอยู่ในระบบ";
            return RedirectToAction(nameof(Index));
        }

        user.Name = model.Name.Trim();
        user.Username = username;
        user.Email = email;
        user.PhoneNumber = model.PhoneNumber.Trim();

        if (user.Staff is null)
        {
            user.Staff = new Staff
            {
                User = user,
                Role = role,
                RoleId = role.Id,
                Status = "active"
            };
        }
        else
        {
            user.Staff.Role = role;
            user.Staff.RoleId = role.Id;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["AdminError"] = "บันทึกการแก้ไขไม่สำเร็จ กรุณาตรวจสอบข้อมูลและลองอีกครั้ง";
            return RedirectToAction(nameof(Index));
        }

        if (HttpContext.Session.GetInt32("AccountId") == user.Id)
        {
            HttpContext.Session.SetString("AccountName", user.Email);
            HttpContext.Session.SetString("AccountRole", role.Name);
        }

        TempData["AdminSuccess"] = "บันทึกข้อมูลผู้ใช้เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var user = await _db.Users
            .Include(account => account.Staff)
            .SingleOrDefaultAsync(account => account.Id == userId);

        if (user is null)
        {
            TempData["AdminError"] = "ไม่พบบัญชีผู้ใช้ที่ต้องการลบ";
            return RedirectToAction(nameof(Index));
        }

        if (user.Staff is not null)
        {
            _db.Staff.Remove(user.Staff);
        }
        _db.Users.Remove(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["AdminError"] = "ลบบัญชีผู้ใช้ไม่สำเร็จ เนื่องจากข้อมูลยังถูกใช้งานอยู่ในระบบ";
            return RedirectToAction(nameof(Index));
        }

        if (HttpContext.Session.GetInt32("AccountId") == user.Id)
        {
            HttpContext.Session.Clear();
        }

        TempData["AdminSuccess"] = "ลบบัญชีผู้ใช้เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Bookings(string? filter)
    {
        var bookings = await _db.Bookings.AsNoTracking()
            .Where(BookingQueue.AppliesTo)
            .Include(b => b.User)
            .OrderByDescending(b => b.Id)
            .ToListAsync();

        return View(new AdminBookingBoardViewModel(bookings, filter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBookingStatus(int id, string status, string? filter)
    {
        var returnFilter = AdminBookingBoardViewModel.NormalizeFilter(filter);
        var reservation = _db.Bookings.Where(BookingQueue.AppliesTo).Where(booking => booking.Id == id);
        var booking = await reservation.Select(item => new { item.Status }).SingleOrDefaultAsync();
        if (booking is null)
        {
            TempData["AdminError"] = "ไม่พบรายการจองนี้ในระบบ";
            return RedirectToAction(nameof(Bookings), new { filter = returnFilter });
        }

        if (!AdminBookingBoardViewModel.CanChange(booking.Status, status))
        {
            TempData["AdminError"] = "รายการนี้เปลี่ยนไปยังสถานะที่เลือกไม่ได้";
            return RedirectToAction(nameof(Bookings), new { filter = returnFilter });
        }

        var changed = await reservation.Where(item => item.Status == booking.Status)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, status));
        if (changed == 1)
            TempData["AdminSuccess"] = $"อัปเดตรายการจอง #{id} เป็น «{AdminBookingBoardViewModel.Describe(status).Label}» แล้ว";
        else
            TempData["AdminError"] = "สถานะรายการจองเปลี่ยนไปแล้ว กรุณาตรวจสอบอีกครั้ง";

        return RedirectToAction(nameof(Bookings), new { filter = returnFilter });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking != null)
        {
            _db.Bookings.Remove(booking);
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"ลบรายการจอง #{id} สำเร็จ";
        }
        return RedirectToAction(nameof(Bookings));
    }

    // ==================== SERVICE ITEMS CRUD ====================

    [HttpGet]
    public async Task<IActionResult> Services(string type = "Promotion")
    {
        var items = await _db.ServiceItems
            .Where(s => s.ServiceType == type)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync();

        ViewBag.ServiceType = type;
        ViewBag.ServiceTypeLabel = GetTypeLabel(type);
        return View(items);
    }

    [HttpGet]
    public IActionResult CreateService(string type = "Promotion")
    {
        var model = new ServiceItemViewModel
        {
            ServiceType = type,
            IsActive = true
        };
        ViewBag.ServiceTypeLabel = GetTypeLabel(type);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateService(ServiceItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ServiceTypeLabel = GetTypeLabel(model.ServiceType);
            return View(model);
        }

        var item = new ServiceItem
        {
            ServiceType = model.ServiceType,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            Price = model.Price,
            OriginalPrice = model.OriginalPrice,
            ImageUrl = model.ImageUrl?.Trim(),
            BadgeText = model.BadgeText?.Trim(),
            BadgeColor = model.BadgeColor?.Trim(),
            IsActive = model.IsActive,
            SortOrder = model.SortOrder
        };

        _db.ServiceItems.Add(item);
        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = $"เพิ่มรายการ \"{item.Name}\" สำเร็จ";
        return RedirectToAction(nameof(Services), new { type = model.ServiceType });
    }

    [HttpGet]
    public async Task<IActionResult> EditService(int id)
    {
        var item = await _db.ServiceItems.FindAsync(id);
        if (item == null)
        {
            TempData["AdminError"] = "ไม่พบรายการบริการที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Services));
        }

        var model = new ServiceItemViewModel
        {
            Id = item.Id,
            ServiceType = item.ServiceType,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            OriginalPrice = item.OriginalPrice,
            ImageUrl = item.ImageUrl,
            BadgeText = item.BadgeText,
            BadgeColor = item.BadgeColor,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder
        };
        ViewBag.ServiceTypeLabel = GetTypeLabel(item.ServiceType);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(ServiceItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ServiceTypeLabel = GetTypeLabel(model.ServiceType);
            return View(model);
        }

        var item = await _db.ServiceItems.FindAsync(model.Id);
        if (item == null)
        {
            TempData["AdminError"] = "ไม่พบรายการบริการที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Services), new { type = model.ServiceType });
        }

        item.Name = model.Name.Trim();
        item.Description = model.Description?.Trim();
        item.Price = model.Price;
        item.OriginalPrice = model.OriginalPrice;
        item.ImageUrl = model.ImageUrl?.Trim();
        item.BadgeText = model.BadgeText?.Trim();
        item.BadgeColor = model.BadgeColor?.Trim();
        item.IsActive = model.IsActive;
        item.SortOrder = model.SortOrder;

        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = $"บันทึกการแก้ไข \"{item.Name}\" สำเร็จ";
        return RedirectToAction(nameof(Services), new { type = model.ServiceType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteService(int id)
    {
        var item = await _db.ServiceItems.FindAsync(id);
        if (item != null)
        {
            var type = item.ServiceType;
            _db.ServiceItems.Remove(item);
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"ลบรายการ \"{item.Name}\" สำเร็จ";
            return RedirectToAction(nameof(Services), new { type });
        }
        return RedirectToAction(nameof(Services));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleServiceActive(int id)
    {
        var item = await _db.ServiceItems.FindAsync(id);
        if (item != null)
        {
            item.IsActive = !item.IsActive;
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = $"เปลี่ยนสถานะ \"{item.Name}\" เป็น {(item.IsActive ? "เปิดใช้งาน" : "ปิดใช้งาน")}";
            return RedirectToAction(nameof(Services), new { type = item.ServiceType });
        }
        return RedirectToAction(nameof(Services));
    }

    private static string GetTypeLabel(string type) => type switch
    {
        "Promotion" => "โปรโมชัน",
        "Makeup" => "ช่างแต่งหน้า",
        "Photographer" => "ช่างภาพ",
        "Costume" => "เช่าชุด",
        _ => type
    };

    private async Task<AdminUserListViewModel> BuildUserListViewModelAsync()
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleOptionViewModel { Id = role.Id, Name = role.Name })
            .ToListAsync();
        var defaultRoleId = roles.FirstOrDefault(role => role.Name == "User")?.Id;
        var users = await _db.Users
            .AsNoTracking()
            .Include(user => user.Staff)
                .ThenInclude(staff => staff!.Role)
            .OrderBy(user => user.Email)
            .ToListAsync();

        return new AdminUserListViewModel
        {
            Users = users.Select(user => new AdminUserRowViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleId = user.Staff?.RoleId ?? defaultRoleId,
                RoleName = user.Staff?.Role?.Name ?? "User"
            }).ToList(),
            Roles = roles
        };
    }

    private async Task LoadRolesAsync(AdminAddUserViewModel model)
    {
        model.Roles = await _db.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new RoleOptionViewModel { Id = role.Id, Name = role.Name })
            .ToListAsync();
    }
}
