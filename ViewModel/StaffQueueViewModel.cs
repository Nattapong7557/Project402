using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.ViewModel;

public class StaffQueueViewModel
{
    public List<Booking> QueueItems { get; set; } = new List<Booking>();

    // สำหรับฟอร์มอัปเดตสถานะคิวด้วย asp-for
    [Required]
    public int SelectedBookingId { get; set; }

    [Required]
    public string NewStatus { get; set; } = string.Empty;

    // สำหรับฟอร์มเพิ่มคิวลูกค้า Walk-in ด้วย asp-for
    [Required(ErrorMessage = "กรุณาเลือกโซนหรือบริการ")]
    public string WalkInServiceType { get; set; } = "Table";

    [Required(ErrorMessage = "กรุณาระบุชื่อโซนหรือรายการ")]
    public string WalkInItemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุรอบเวลา")]
    public string WalkInTimeSlot { get; set; } = "13:00 - 15:00 น.";

    public int WalkInGuestCount { get; set; } = 2;

    public string? WalkInNote { get; set; }
}
