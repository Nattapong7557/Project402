using Nattapong_7379_L002.Models.FoodOrdering;

namespace Nattapong_7379_L002.ViewModel.FoodOrdering;

public sealed record FoodOrderSection(string Key, string Title, string EmptyMessage, string Icon, IReadOnlyList<FoodOrder> Orders);
public sealed record FoodTicketViewModel(FoodOrder Order, bool ShowTotals = true, bool BirthdayPending = false);

public static class FoodOrderSections
{
    public static IReadOnlyList<FoodOrderSection> ForCustomer(FoodOrderListViewModel model) =>
        model.Bills
            ? [
                Create(model.Orders, "ready-bills", "พร้อมขอเช็คบิล", "ยังไม่มีออเดอร์ที่พร้อมเช็คบิล", "bill", order => order.Status == FoodOrderStatus.Served),
                Create(model.Orders, "pending-bills", "รอยืนยันชำระ", "ยังไม่มีบิลรอยืนยันชำระ", "clock", order => order.Status == FoodOrderStatus.AwaitingPayment),
                Create(model.Orders, "paid-bills", "ชำระแล้ว", "ยังไม่มีบิลที่ชำระแล้ว", "check", order => order.Status == FoodOrderStatus.Paid)
            ]
            : [
                Create(model.Orders, "current-orders", "ออเดอร์ที่กำลังดำเนินการ", "ยังไม่มีออเดอร์ที่กำลังดำเนินการ", "clock", order => order.Status != FoodOrderStatus.Paid),
                Create(model.Orders, "past-orders", "ออเดอร์ที่เสร็จแล้ว", "ยังไม่มีออเดอร์ที่เสร็จแล้ว", "check", order => order.Status == FoodOrderStatus.Paid)
            ];

    public static IReadOnlyList<FoodOrderSection> ForStaff(FoodStaffViewModel model) => model.Workspace switch
    {
        "Cashier" => [
            Create(model.Orders, "incoming", "รอยืนยันส่งครัว", "ยังไม่มีออเดอร์รอยืนยัน", "orders", order => order.Status == FoodOrderStatus.Submitted),
            Create(model.Orders, "serving", "พร้อมเสิร์ฟ", "ยังไม่มีออเดอร์พร้อมเสิร์ฟ", "food", order => order.Status == FoodOrderStatus.Ready)
        ],
        "Kitchen" => [
            Create(model.Orders, "preparing", "รายการที่ต้องทำ", "ยังไม่มีออเดอร์ที่ต้องทำ", "food", order => order.Status == FoodOrderStatus.InKitchen)
        ],
        "Accounting" => [
            Create(model.Orders, "unpaid", "บิลรอยืนยันชำระ", "ยังไม่มีบิลรอยืนยันชำระ", "bill", order => order.Status == FoodOrderStatus.AwaitingPayment),
            Create(model.Orders, "payments", "ประวัติการชำระล่าสุด", "ยังไม่มีประวัติการชำระ", "check", order => order.Status == FoodOrderStatus.Paid)
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(model))
    };

    private static FoodOrderSection Create(IEnumerable<FoodOrder> orders, string key, string title, string empty,
        string icon, Func<FoodOrder, bool> include) => new(key, title, empty, icon, orders.Where(include).ToList());
}
