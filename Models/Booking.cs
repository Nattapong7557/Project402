using System;

namespace Nattapong_7379_L002.Models;

public partial class Booking
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ServiceType { get; set; } = null!; // Table, Makeup, Photographer, Costume, Promotion

    public string ItemName { get; set; } = null!;

    public string BookingDate { get; set; } = null!;

    public string TimeSlot { get; set; } = null!;

    public int GuestCount { get; set; } = 1;

    public string? Note { get; set; }

    public string Status { get; set; } = "Confirmed";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public FoodOrdering.BookingCharge? Charge { get; set; }
}

