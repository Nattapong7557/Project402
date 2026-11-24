using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models.FoodOrdering;

namespace Nattapong_7379_L002.Models;

public partial class Csi402dbContext
{
    public DbSet<FoodMenuItem> FoodMenuItems => Set<FoodMenuItem>();
    public DbSet<FoodOrder> FoodOrders => Set<FoodOrder>();
    public DbSet<FoodPayment> FoodPayments => Set<FoodPayment>();
    public DbSet<FoodCustomerProfile> FoodCustomerProfiles => Set<FoodCustomerProfile>();
    public DbSet<FoodBirthdayRedemption> FoodBirthdayRedemptions => Set<FoodBirthdayRedemption>();
    public DbSet<BookingCharge> BookingCharges => Set<BookingCharge>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FoodOrder>(entity =>
        {
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(order => new { order.UserId, order.RequestToken }).IsUnique();
            entity.HasMany(order => order.Items).WithOne().HasForeignKey(item => item.OrderId);
            entity.HasOne(order => order.Payment).WithOne().HasForeignKey<FoodPayment>(payment => payment.OrderId);
        });
        modelBuilder.Entity<FoodCustomerProfile>().Property(profile => profile.UserId).ValueGeneratedNever();
        modelBuilder.Entity<BookingCharge>().HasOne(charge => charge.Booking).WithOne(booking => booking.Charge)
            .HasForeignKey<BookingCharge>(charge => charge.BookingId);
    }
}
