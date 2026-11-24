using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Nattapong_7379_L002.Models.FoodOrdering;

[Table("food_customer_profiles")]
public sealed class FoodCustomerProfile
{
    [Key, Column("user_id")] public int UserId { get; set; }
    [Column("birth_date")] public DateOnly BirthDate { get; set; }
}

[PrimaryKey(nameof(UserId), nameof(Year))]
[Table("food_birthday_redemptions")]
public sealed class FoodBirthdayRedemption
{
    [Column("user_id")] public int UserId { get; set; }
    [Column("redemption_year")] public int Year { get; set; }
    [Column("order_id")] public int OrderId { get; set; }
    public FoodOrder Order { get; set; } = null!;
}

[Table("booking_charges")]
public sealed class BookingCharge
{
    [Key, Column("booking_id")] public int BookingId { get; set; }
    [Column("service_item_id")] public int? ServiceItemId { get; set; }
    [Column("subtotal"), Precision(10, 2)] public decimal Subtotal { get; set; }
    [Column("discount"), Precision(10, 2)] public decimal Discount { get; set; }
    [Column("total"), Precision(10, 2)] public decimal Total { get; set; }
    public Booking Booking { get; set; } = null!;
}
