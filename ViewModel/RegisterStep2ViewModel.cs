using System.ComponentModel.DataAnnotations;

namespace Nattapong_7379_L002.ViewModel;

public sealed class RegisterStep2ViewModel
{
    [Required(ErrorMessage = "กรุณาเลือกแพ็กเกจ")]
    public string SelectedPackage { get; set; } = string.Empty;
}

