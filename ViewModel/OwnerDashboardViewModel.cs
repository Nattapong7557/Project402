using System.Collections.Generic;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.ViewModel;

public class OwnerDashboardViewModel
{
    public required MonthlyProfitViewModel Profit { get; init; }

    public int TotalBookingsCount { get; set; }

    public int TotalUsersCount { get; set; }

    public int TotalServicesCount { get; set; }

    public List<Booking> RecentBookings { get; set; } = new List<Booking>();
}
