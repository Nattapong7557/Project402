using Nattapong_7379_L002.Models.FoodOrdering;

namespace Nattapong_7379_L002.Services.FoodOrdering;

public enum FoodOrderAction { Accept, MarkReady, Serve, RequestBill, Pay }

public static class FoodOrderRules
{
    public const string Cashier = "Cashiers and staff";
    public const string Kitchen = "Kitchen staff";
    public const string Accounting = "Accounting";

    public static bool CanOrder(string? role) => role is "User" or "Admin";

    public static bool IsStaff(string? role, string required) => role == required || role == "Admin";

    public static (FoodOrderStatus From, FoodOrderStatus To, string Role) Transition(FoodOrderAction action) => action switch
    {
        FoodOrderAction.Accept => (FoodOrderStatus.Submitted, FoodOrderStatus.InKitchen, Cashier),
        FoodOrderAction.MarkReady => (FoodOrderStatus.InKitchen, FoodOrderStatus.Ready, Kitchen),
        FoodOrderAction.Serve => (FoodOrderStatus.Ready, FoodOrderStatus.Served, Cashier),
        FoodOrderAction.RequestBill => (FoodOrderStatus.Served, FoodOrderStatus.AwaitingPayment, "User"),
        FoodOrderAction.Pay => (FoodOrderStatus.AwaitingPayment, FoodOrderStatus.Paid, Accounting),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static string StatusLabel(FoodOrderStatus status) => status switch
    {
        FoodOrderStatus.Submitted => "รอแคชเชียร์ยืนยัน",
        FoodOrderStatus.InKitchen => "ครัวกำลังเตรียมอาหาร",
        FoodOrderStatus.Ready => "อาหารพร้อมเสิร์ฟ",
        FoodOrderStatus.Served => "เสิร์ฟแล้ว",
        FoodOrderStatus.AwaitingPayment => "รอบัญชียืนยันชำระ",
        FoodOrderStatus.Paid => "ชำระแล้ว",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
