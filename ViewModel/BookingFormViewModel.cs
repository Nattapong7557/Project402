using System.ComponentModel.DataAnnotations;

namespace Nattapong_7379_L002.ViewModel;

public class BookingFormViewModel
{
    [Required]
    public string ServiceType { get; set; } = "Table"; // Table, Makeup, Photographer, Costume, Promotion

    [Required(ErrorMessage = "กรุณาระบุรายการหรือชื่อบริการ")]
    public string ItemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกวันที่ต้องการจอง")]
    public string BookingDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกรอบเวลา")]
    public string TimeSlot { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "จำนวนคนต้องอยู่ระหว่าง 1 - 20 คน")]
    public int GuestCount { get; set; } = 1;

    public string? Note { get; set; }

    public string? ImageUrl { get; set; }

    public decimal Price { get; set; } = 0;

    public int? ServiceItemId { get; set; }
}

