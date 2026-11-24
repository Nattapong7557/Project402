using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.ViewModel;

public class CashierViewModel
{
    public List<Booking> Bookings { get; set; } = new List<Booking>();

    public int OpenTablesCount { get; set; }

    public int PaidTablesCount { get; set; }

    // สำหรับรับค่าจากฟอร์มด้วย asp-for
    [Required]
    public int SelectedBookingId { get; set; }

    [Required]
    public string NewStatus { get; set; } = string.Empty;

    // สำหรับฟอร์มเปิดโต๊ะ/เพิ่มรายการหน้าร้านใหม่ด้วย asp-for
    [Required(ErrorMessage = "กรุณาระบุชื่อโต๊ะหรือรายการ")]
    public string NewTableItemName { get; set; } = string.Empty;

    public int NewTableGuestCount { get; set; } = 2;

    public string? NewTableNote { get; set; }
}
