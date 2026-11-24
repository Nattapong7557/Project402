using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Nattapong_7379_L002.Services.Membership;
using Nattapong_7379_L002.Models;
using Nattapong_7379_L002.ViewModel;

namespace Nattapong_7379_L002.Controllers;

public class AccountController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly TimeProvider _clock;

    public AccountController(Csi402dbContext db, IPasswordHasher<User> passwordHasher, TimeProvider clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public IActionResult Index()
    {
        ViewBag.AccountName = HttpContext.Session.GetString("AccountName");
        return View();
    }

     public IActionResult Lab4()
    {
        return View();
    }
   
    [HttpGet]
    public IActionResult Register()
    {
        ViewData["Title"] = "สมัครสมาชิก - Step 1";
        var model = new RegisterStep1ViewModel
        {
            Name = HttpContext.Session.GetString("Reg_Name") ?? string.Empty,
            Username = HttpContext.Session.GetString("Reg_Username") ?? string.Empty,
            Email = HttpContext.Session.GetString("Reg_Email") ?? string.Empty,
            PhoneNumber = HttpContext.Session.GetString("Reg_Phone") ?? string.Empty
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterStep1(RegisterStep1ViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "สมัครสมาชิก - Step 1";
            return View("Register", model);
        }

        var email = model.Email.Trim();
        var username = model.Username.Trim();

        if (await _db.Users.AnyAsync(user => user.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีบัญชีในระบบแล้ว");
            ViewData["Title"] = "สมัครสมาชิก - Step 1";
            return View("Register", model);
        }

        if (await _db.Users.AnyAsync(user => user.Username == username))
        {
            ModelState.AddModelError(nameof(model.Username), "ชื่อผู้ใช้นี้ถูกใช้งานแล้ว");
            ViewData["Title"] = "สมัครสมาชิก - Step 1";
            return View("Register", model);
        }

        // เก็บข้อมูล Step 1 ไว้ใน Session เพื่อส่งต่อไป Step 2
        HttpContext.Session.SetString("Reg_Name", model.Name.Trim());
        HttpContext.Session.SetString("Reg_Username", username);
        HttpContext.Session.SetString("Reg_Email", email);
        HttpContext.Session.SetString("Reg_Phone", model.PhoneNumber.Trim());
        HttpContext.Session.SetString("Reg_Password", model.Password);
        HttpContext.Session.Remove("Reg_Package");

        return RedirectToAction(nameof(RegisterStep2));
    }

    [HttpGet]
    public IActionResult RegisterStep2()
    {
        var email = HttpContext.Session.GetString("Reg_Email");
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Register));
        }

        ViewData["Title"] = "เลือกแพ็กเกจ - Step 2";
        return View(new RegisterStep2ViewModel { SelectedPackage = MembershipRules.PremiumPackage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegisterStep2(RegisterStep2ViewModel model)
    {
        var email = HttpContext.Session.GetString("Reg_Email");
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Register));
        }

        if (!ModelState.IsValid || !MembershipRules.CanSelectSignupPackage(model.SelectedPackage))
        {
            HttpContext.Session.Remove("Reg_Package");
            return SelectAvailablePackage();
        }

        HttpContext.Session.SetString("Reg_Package", MembershipRules.PremiumPackage);

        return RedirectToAction(nameof(RegisterStep3));
    }

    [HttpGet]
    public IActionResult RegisterStep3()
    {
        var email = HttpContext.Session.GetString("Reg_Email");
        var name = HttpContext.Session.GetString("Reg_Name");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
        {
            return RedirectToAction(nameof(Register));
        }

        var pkg = HttpContext.Session.GetString("Reg_Package");
        if (!MembershipRules.CanSelectSignupPackage(pkg))
            return SelectAvailablePackage();

        ViewData["Title"] = "ชำระเงินและยืนยัน - Step 3";

        var model = new RegisterStep3ViewModel
        {
            Name = name,
            Email = email,
            SelectedPackage = MembershipRules.PremiumPackage,
            PackagePrice = MembershipRules.SignupPrice(pkg)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmRegister()
    {
        var name = HttpContext.Session.GetString("Reg_Name");
        var username = HttpContext.Session.GetString("Reg_Username");
        var email = HttpContext.Session.GetString("Reg_Email");
        var phone = HttpContext.Session.GetString("Reg_Phone");
        var password = HttpContext.Session.GetString("Reg_Password");
        var package = HttpContext.Session.GetString("Reg_Package");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
        {
            return RedirectToAction(nameof(Register));
        }

        if (!MembershipRules.CanSelectSignupPackage(package))
            return SelectAvailablePackage();

        // ตรวจสอบซ้ำอีกครั้ง
        if (await _db.Users.AnyAsync(u => u.Email == email || u.Username == username))
        {
            TempData["RegisterError"] = "อีเมลหรือชื่อผู้ใช้นี้ถูกใช้งานแล้ว";
            return RedirectToAction(nameof(Register));
        }

        var user = new User
        {
            Name = name ?? username,
            Username = username,
            Email = email,
            PhoneNumber = phone ?? string.Empty,
            Package = MembershipRules.PremiumPackage,
            PasswordHash = string.Empty
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _db.Users.Add(user);
        if (MembershipRules.CreateSignupPayment(user, _clock.GetUtcNow().UtcDateTime) is { } payment)
            _db.MembershipPayments.Add(payment);

        try
        {
            // The account and its payment use one database transaction.
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 })
        {
            TempData["RegisterError"] = "อีเมลหรือชื่อผู้ใช้นี้ถูกใช้งานแล้ว";
            return RedirectToAction(nameof(Register));
        }

        // ล้างข้อมูลการสมัครชั่วคราว
        HttpContext.Session.Remove("Reg_Name");
        HttpContext.Session.Remove("Reg_Username");
        HttpContext.Session.Remove("Reg_Email");
        HttpContext.Session.Remove("Reg_Phone");
        HttpContext.Session.Remove("Reg_Password");
        HttpContext.Session.Remove("Reg_Package");

        // เข้าสู่ระบบให้อัตโนมัติทันที
        HttpContext.Session.SetString("AccountName", user.Email);
        HttpContext.Session.SetString("AccountDisplayName", user.Name);
        HttpContext.Session.SetString("AccountRole", "User");
        HttpContext.Session.SetString("AccountPackage", user.Package ?? "Premium");
        HttpContext.Session.SetInt32("AccountId", user.Id);

        TempData["WelcomeMessage"] = $"ยินดีต้อนรับคุณ {user.Name}! บัญชีสมาชิกแพ็กเกจ {user.Package} เปิดใช้งานเรียบร้อยแล้ว ปลดล็อกทุกบริการของสตูดิโอให้คุณแล้วครับ";
        return RedirectToAction("Index", "Home");
    }

    private IActionResult SelectAvailablePackage()
    {
        TempData["SignupPackageError"] = "ตอนนี้เปิดรับสมัครเฉพาะ Premium กรุณาเลือกแพ็กเกจแล้วกดต่อไป";
        return RedirectToAction(nameof(RegisterStep2));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterViewModel model)
    {
        // รักษา backward compatibility หากมีฟอร์มเก่าส่งมา
        var step1Model = new RegisterStep1ViewModel
        {
            Name = model.Name,
            Username = model.Username,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword
        };
        return await RegisterStep1(step1Model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["Title"] = "เข้าสู่ระบบ";
        return View(new LoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var loginInput = model.Email.Trim();
        var account = await _db.Users
            .Include(user => user.Staff)
                .ThenInclude(staff => staff!.Role)
            .SingleOrDefaultAsync(user => user.Email == loginInput || user.Username == loginInput);

        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
            ViewData["Title"] = "เข้าสู่ระบบ";
            return View(model);
        }

        var verification = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, model.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
            ViewData["Title"] = "เข้าสู่ระบบ";
            return View(model);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _passwordHasher.HashPassword(account, model.Password);
            await _db.SaveChangesAsync();
        }

        var roleName = account.Staff?.Role?.Name ?? "User";
        HttpContext.Session.SetString("AccountName", account.Email);
        HttpContext.Session.SetString("AccountDisplayName", account.Name);
        HttpContext.Session.SetString("AccountRole", roleName);
        HttpContext.Session.SetString("AccountPackage", account.Package ?? "Premium");
        HttpContext.Session.SetInt32("AccountId", account.Id);

        TempData["WelcomeMessage"] = $"ยินดีต้อนรับกลับมาครับคุณ {account.Name} (บทบาท: {roleName})";

        if (roleName is "User" or "Admin" && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl!);

        if (roleName == "Admin") return RedirectToAction("Index", "Admin");
        var workspace = Services.FoodOrdering.StaffWorkspace.ForRole(roleName);
        return workspace is null ? RedirectToAction("Index", "Home") : RedirectToAction(workspace.Action, workspace.Controller);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LeaveAccount()
    {
        HttpContext.Session.Remove("AccountName");
        HttpContext.Session.Remove("AccountDisplayName");
        HttpContext.Session.Remove("AccountRole");
        HttpContext.Session.Remove("AccountPackage");
        HttpContext.Session.Remove("AccountId");
        TempData["WelcomeMessage"] = "คุณได้ออกจากระบบเรียบร้อยแล้ว";
        return RedirectToAction("Index", "Home");
    }
}
