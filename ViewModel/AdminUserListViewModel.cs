namespace Nattapong_7379_L002.ViewModel;

using System.ComponentModel.DataAnnotations;

public sealed class AdminUserListViewModel
{
    public IReadOnlyList<AdminUserRowViewModel> Users { get; init; } = Array.Empty<AdminUserRowViewModel>();
    public IReadOnlyList<RoleOptionViewModel> Roles { get; init; } = Array.Empty<RoleOptionViewModel>();
}

public sealed class AdminUserRowViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public int? RoleId { get; init; }
    public string RoleName { get; init; } = "User";
}

public sealed class AdminEditUserViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "ไม่พบบัญชีผู้ใช้")]
    public int Id { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อ")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
    [StringLength(20, ErrorMessage = "เบอร์โทรศัพท์ต้องไม่เกิน 20 ตัวอักษร")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือก Role")]
    public int RoleId { get; set; }
}

public sealed class RoleOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}