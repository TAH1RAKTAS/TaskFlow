using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs;

public class UpdateTaskStatusDto
{
    [Required(ErrorMessage = "Görev durumu zorunludur.")]
    [RegularExpression("^(Başlamadı|Devam Ediyor|Tamamlandı)$", ErrorMessage = "Geçerli bir görev durumu seçin.")]
    public string Status { get; set; } = string.Empty;
}
