using Microsoft.EntityFrameworkCore;
using Nattapong_7379_L002.Models.Membership;

namespace Nattapong_7379_L002.Models;

public partial class Csi402dbContext
{
    public DbSet<MembershipPayment> MembershipPayments => Set<MembershipPayment>();

    private static void ConfigureMembership(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MembershipPayment>().HasOne(payment => payment.Member)
            .WithOne().HasForeignKey<MembershipPayment>(payment => payment.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
