using System.Linq.Expressions;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.Services;

public static class TableBookingWorkflow
{
    public static Expression<Func<Booking, bool>> ForKitchen => booking =>
        (booking.ServiceType == "Table" || booking.ServiceType == "Kitchen") &&
        (booking.Status == "QueueConfirmed" || booking.Status == "Confirmed" ||
         booking.Status == "Checked-In" || booking.Status == "Preparing" ||
         booking.Status == "Ready" || booking.Status == "Served");

    public static Expression<Func<Booking, bool>> ForCashier => booking =>
        booking.ServiceType == "Table" &&
        (booking.Status == "QueueConfirmed" || booking.Status == "Confirmed" ||
         booking.Status == "Checked-In" || booking.Status == "Preparing" || booking.Status == "Ready" ||
         booking.Status == "Served" || booking.Status == "Paid" || booking.Status == "Completed");

    public static string? NextForKitchen(Booking booking) => (booking.ServiceType, booking.Status) switch
    {
        ("Table" or "Kitchen", "QueueConfirmed" or "Confirmed" or "Checked-In") => "Preparing",
        ("Table" or "Kitchen", "Preparing" or "Ready") => "Served",
        _ => null
    };

    public static string? NextForCashier(Booking booking) => (booking.ServiceType, booking.Status) switch
    {
        ("Table", "Served") => "Paid",
        ("Table", "Paid") => "Completed",
        _ => null
    };
}
