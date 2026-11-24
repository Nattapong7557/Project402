using System.Linq.Expressions;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.Services;

public static class BookingQueue
{
    public static Expression<Func<Booking, bool>> AppliesTo => booking =>
        booking.ServiceType == "Table" || booking.ServiceType == "Promotion" || booking.ServiceType == "Makeup" ||
        booking.ServiceType == "Photographer" || booking.ServiceType == "Costume";

    public static bool AllowsWalkIn(string type) => type is "Table" or "Photographer" or "Makeup" or "Costume";
    public static bool IsQueueDecision(string status) => status is "QueueConfirmed" or "Cancelled";
}
