using System.ComponentModel.DataAnnotations;
using Nattapong_7379_L002.Models.FoodOrdering;
using Nattapong_7379_L002.ViewModel.Membership;

namespace Nattapong_7379_L002.ViewModel.FoodOrdering;

public sealed class FoodMenuViewModel
{
    public IReadOnlyList<FoodMenuItem> Menu { get; set; } = [];
    public FoodOrderInput Order { get; set; } = new();
    public bool CanOrder { get; set; }
}

public sealed class FoodOrderInput
{
    [Required]
    public string RequestToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุหมายเลขโต๊ะ"), Range(1, 99, ErrorMessage = "หมายเลขโต๊ะต้องอยู่ระหว่าง 1–99")]
    public int? TableNumber { get; set; }


    public List<FoodOrderLineInput> Lines { get; set; } = [];
}

public sealed class FoodOrderLineInput
{
    public int MenuItemId { get; set; }

    [Range(0, 20)]
    public int Quantity { get; set; }
}

public sealed class FoodOrderListViewModel
{
    public bool Bills { get; set; }
    public bool NeedsBirthDate { get; set; }
    public IReadOnlyList<FoodOrder> Orders { get; set; } = [];
}

public sealed class FoodStaffViewModel
{
    public MonthlyProfitViewModel? Profit { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Workspace { get; set; } = string.Empty;
    public List<FoodOrder> Orders { get; set; } = [];
    public decimal PaidRevenue { get; set; }
    public MembershipRevenueViewModel? Membership { get; set; }
}
