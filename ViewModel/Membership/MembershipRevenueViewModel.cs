using Nattapong_7379_L002.Models.Membership;

namespace Nattapong_7379_L002.ViewModel.Membership;

public sealed class MembershipRevenueViewModel
{
    public decimal Total { get; set; }
    public decimal MonthlyTotal { get; set; }
    public IReadOnlyList<MembershipPayment> Payments { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageCount { get; set; } = 1;
}
