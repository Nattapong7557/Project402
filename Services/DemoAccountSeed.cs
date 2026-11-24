using Microsoft.AspNetCore.Identity;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.Services;

public static class DemoAccountSeed
{
    public static string? ResolvePassword(bool isDevelopment, IConfiguration configuration)
    {
        if (!isDevelopment || !configuration.GetValue<bool>("DemoAccounts:Enabled"))
            return null;

        var password = configuration["DemoAccounts:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 100)
            throw new InvalidOperationException("Configure DemoAccounts:Password with 12 to 100 characters.");

        return password;
    }

    internal static DemoAccount[] Accounts { get; } =
    [
        new("Admin", "ผู้ดูแลระบบ (Admin)", "admin", "081-234-5678", "Admin VIP"),
        new("Staff queue", "พนักงานจัดคิว (Staff Queue)", "staffqueue", "081-111-0001"),
        new("Kitchen staff", "พนักงานครัว (Kitchen Staff)", "staffkitchen", "081-111-0002"),
        new("Cashiers and staff", "พนักงานแคชเชียร์ (Cashier)", "staffcashier", "081-111-0003"),
        new("Accounting", "พนักงานบัญชี (Accounting)", "staffaccount", "081-111-0004"),
        new("Owner", "เจ้าของร้าน (Owner)", "owner", "081-111-0005")
    ];
}

internal sealed record DemoAccount(string RoleName, string Name, string Username, string Phone, string Package = "Premium")
{
    public string Email => $"{Username}@gmail.com";

    public Staff CreateStaff(Role role, IPasswordHasher<User> hasher, string password)
    {
        var user = new User { Name = Name, Username = Username, Email = Email, PhoneNumber = Phone, Package = Package };
        user.PasswordHash = hasher.HashPassword(user, password);
        return new Staff { User = user, Role = role, Status = "active" };
    }
}
