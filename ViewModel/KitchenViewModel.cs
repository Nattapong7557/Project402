using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Nattapong_7379_L002.Models;

namespace Nattapong_7379_L002.ViewModel;

public class KitchenViewModel
{
    public List<Booking> KitchenOrders { get; set; } = new List<Booking>();

    public int PreparingCount { get; set; }

    public int ReadyCount { get; set; }

    // สำหรับฟอร์มอัปเดตสถานะอาหารด้วย asp-for
    [Required]
    public int SelectedOrderId { get; set; }

    [Required]
    public string NewStatus { get; set; } = string.Empty;

    // สำหรับฟอร์มเพิ่มออเดอร์เข้าครัวด้วย asp-for
    [Required(ErrorMessage = "กรุณาระบุหมายเลขโต๊ะ")]
    public string TableNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุรายการอาหาร/เครื่องดื่ม")]
    public string FoodItems { get; set; } = string.Empty;
}
