namespace Nattapong_7379_L002.Services.FoodOrdering;

public sealed record StaffWorkspace(string Controller, string Action, string Label, string Icon)
{
    public static StaffWorkspace? ForRole(string? role) => role?.ToUpperInvariant() switch
    {
        "CASHIERS AND STAFF" => new("FoodStaff", "Cashier", "แคชเชียร์", "basket"),
        "KITCHEN STAFF" => new("FoodStaff", "Kitchen", "ครัว", "food"),
        "ACCOUNTING" => new("FoodStaff", "Accounting", "บัญชีร้าน", "bill"),
        "STAFF QUEUE" => new("StaffQueue", "Index", "จัดคิวจอง", "calendar"),
        "OWNER" => new("Owner", "Index", "ภาพรวมร้าน", "chart"),
        _ => null
    };

    public static bool IsWorkspaceController(string? controller) =>
        controller is "FoodStaff" or "StaffQueue" or "Owner" or "Cashier" or "Kitchen" or "Accounting" or "Admin" or "MembershipRevenue";
}
