using System.ComponentModel.DataAnnotations;
// Validation attribute'larını kullanabilmek için
namespace TaskFlow.DTOs;
public class TaskItemDto
{
    public int Id { get; set; }
    // Task'ın ID bilgisini taşır.
    [Required(ErrorMessage = "Görev başlığı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Görev başlığı en fazla 100 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;
    // Task'ın başlığını taşır ve doğrular.
    [Required(ErrorMessage = "Görev açıklaması zorunludur.")]
    [MaxLength(100, ErrorMessage = "Görev açıklaması en fazla 100 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;
    // Task'ın açıklamasını taşır ve doğrular.
    [Required(ErrorMessage = "Öncelik seviyesi zorunludur.")]
    [RegularExpression("^(Düşük|Orta|Yüksek)$", ErrorMessage = "Öncelik Düşük, Orta veya Yüksek olmalıdır.")]
    public string Priority { get; set; } = "Orta";
    // Task'ın öncelik seviyesini taşır ve doğrular.
    [Required(ErrorMessage = "Görev durumu zorunludur.")]
    [RegularExpression("^(Başlamadı|Devam Ediyor|Tamamlandı)$", ErrorMessage = "Geçerli bir görev durumu seçin.")]
    public string Status { get; set; } = "Başlamadı";
    public DateTime? DueDate { get; set; }
}
