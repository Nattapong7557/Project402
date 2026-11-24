using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Nattapong_7379_L002.Models.Membership;

[Table("membership_payments")]
[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(PaidAt), nameof(Id))]
public sealed class MembershipPayment
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }
    public User? Member { get; set; }

    [Column("member_name"), MaxLength(100)]
    public string MemberName { get; set; } = string.Empty;

    [Column("package"), MaxLength(50)]
    public string Package { get; set; } = string.Empty;

    [Column("amount"), Precision(10, 2)]
    public decimal Amount { get; set; }

    [Column("paid_at", TypeName = "datetime(6)")]
    public DateTime PaidAt { get; set; }
}
