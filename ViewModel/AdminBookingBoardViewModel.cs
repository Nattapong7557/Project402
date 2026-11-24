using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.ViewModel;

public sealed record BookingBoardStatus(string Label, string Group, IReadOnlyList<string> AllowedChanges);
public sealed record BookingBoardFilter(string Key, string Label, string Icon, int Count);
public sealed record BookingBoardRow(Booking Booking, BookingBoardStatus Status);

public sealed class AdminBookingBoardViewModel(IEnumerable<Booking> bookings, string? filter)
{
    private static readonly (string Key, string Label, string Icon)[] FilterDefinitions =
    [
        ("all", "รายการจองทั้งหมด", "calendar"),
        ("pending", "รออนุมัติ", "clock"),
        ("active", "กำลังดำเนินการ", "orders"),
        ("paid", "ชำระแล้ว", "bill"),
        ("completed", "เสร็จสิ้น", "check"),
        ("cancelled", "ยกเลิกแล้ว", "exit"),
        ("other", "สถานะอื่น", "orders")
    ];
    private static readonly IReadOnlyList<string> PendingChanges = Array.AsReadOnly(new[] { "Confirmed", "Cancelled" });
    private static readonly IReadOnlyList<string> ActiveChanges = Array.AsReadOnly(new[] { "Completed", "Cancelled" });
    private static readonly IReadOnlyList<string> PaidChanges = Array.AsReadOnly(new[] { "Completed" });

    private readonly List<BookingBoardRow> _rows = bookings.Select(booking => new BookingBoardRow(booking, Describe(booking.Status))).ToList();

    public string SelectedFilter { get; } = NormalizeFilter(filter);

    public IReadOnlyList<BookingBoardRow> Rows => SelectedFilter == "all"
        ? _rows
        : _rows.Where(row => row.Status.Group == SelectedFilter).ToList();

    public IReadOnlyList<BookingBoardFilter> Filters => FilterDefinitions
        .Select(item => new BookingBoardFilter(item.Key, item.Label, item.Icon,
            item.Key == "all" ? _rows.Count : _rows.Count(row => row.Status.Group == item.Key)))
        .Where(item => item.Key != "other" || item.Count > 0 || SelectedFilter == "other")
        .ToList();

    public static string NormalizeFilter(string? filter) =>
        FilterDefinitions.Any(item => item.Key == filter) ? filter! : "all";

    public static BookingBoardStatus Describe(string? status) => status switch
    {
        "Pending" => new("รออนุมัติ", "pending", PendingChanges),
        "Confirmed" => new("ยืนยันแล้ว", "active", ActiveChanges),
        "QueueConfirmed" => new("รับคิวแล้ว", "active", ActiveChanges),
        "Checked-In" => new("เช็คอินแล้ว", "active", ActiveChanges),
        "Preparing" => new("กำลังเตรียม", "active", ActiveChanges),
        "Ready" => new("พร้อมให้บริการ", "active", ActiveChanges),
        "Served" => new("เสิร์ฟแล้ว", "active", ActiveChanges),
        "Paid" => new("ชำระแล้ว", "paid", PaidChanges),
        "Completed" => new("เสร็จสิ้น", "completed", []),
        "Cancelled" => new("ยกเลิกแล้ว", "cancelled", []),
        _ => new(string.IsNullOrWhiteSpace(status) ? "ไม่ระบุสถานะ" : status, "other", [])
    };

    public static bool CanChange(string? current, string? requested) =>
        requested is not null && Describe(current).AllowedChanges.Contains(requested);
}
