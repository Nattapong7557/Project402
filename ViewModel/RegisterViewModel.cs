using System.ComponentModel.DataAnnotations;

namespace Nattapong_7379_L002.ViewModel;

public sealed class RegisterViewModel
{
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

	[Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
	[StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
	[DataType(DataType.Password)]
	[Compare(nameof(Password), ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
	public string ConfirmPassword { get; set; } = string.Empty;
}
