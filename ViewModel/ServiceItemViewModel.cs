using System.ComponentModel.DataAnnotations;

namespace Nattapong_7379_L002.ViewModel;

public sealed class ServiceItemViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกประเภทบริการ")]
    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อรายการ")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "กรุณากรอกราคา")]
    [Range(0, 999999, ErrorMessage = "ราคาต้องอยู่ระหว่าง 0 - 999,999")]
    public decimal Price { get; set; }

    [Range(0, 999999)]
    public decimal? OriginalPrice { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [StringLength(50)]
    public string? BadgeText { get; set; }

    [StringLength(30)]
    public string? BadgeColor { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
