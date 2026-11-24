using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.ViewModel;

public class AccountingViewModel
{
    public List<Booking> Transactions { get; set; } = new List<Booking>();
    public MonthlyProfitViewModel? Profit { get; init; }

    // สำหรับฟอร์มบันทึกรายการรับ-จ่ายใหม่ด้วย asp-for
    [Required(ErrorMessage = "กรุณาเลือกประเภทรายการ")]
    public string RecordType { get; set; } = "Income";

    [Required(ErrorMessage = "กรุณากรอกชื่อรายการบัญชี")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกจำนวนเงิน")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}
