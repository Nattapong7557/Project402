using System.ComponentModel.DataAnnotations;

namespace Nattapong_7379_L002.ViewModel;

public sealed class LoginViewModel
{
	[Required(ErrorMessage = "กรุณากรอกอีเมลหรือชื่อผู้ใช้")]
	public string Email { get; set; } = string.Empty;

	[Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	public string? ReturnUrl { get; set; }
}
