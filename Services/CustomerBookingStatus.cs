using System.Linq.Expressions;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.Services;

public sealed record CustomerBookingStatus(string Label, string BadgeClass, int Progress, bool CanCancel)
{
    private static readonly string[] CancellableStatuses = ["Pending", "QueueConfirmed", "Confirmed"];

    public static Expression<Func<Booking, bool>> Cancellable => booking =>
        Enumerable.Contains(CancellableStatuses, booking.Status);

    public static CustomerBookingStatus Describe(string status)
    {
        var (label, badge, progress) = status switch
        {
            "Pending" => ("1. รออนุมัติรับคิว", "amber", 20),
            "QueueConfirmed" or "Confirmed" or "Checked-In" => ("2. อนุมัติรับคิวแล้ว", "blue", 45),
            "Preparing" => ("3. กำลังจัดเตรียม", "amber", 65),
            "Ready" => ("พร้อมเสิร์ฟ", "blue", 65),
            "Served" => ("4. เสิร์ฟแล้ว / พร้อมบริการ", "green", 85),
            "Paid" => ("5. ชำระเงินแล้ว", "green", 100),
            "Completed" => ("5. ปิดรายการแล้ว", "green", 100),
            "Cancelled" => ("ยกเลิกคิวแล้ว", "rose", 0),
            _ => ("ไม่ทราบสถานะ", "amber", 0)
        };

        return new CustomerBookingStatus(label, $"ff-card-workspace-badge-{badge}", progress, CancellableStatuses.Contains(status));
    }
}
