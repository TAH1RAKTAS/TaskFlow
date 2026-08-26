using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs;

// Kullanıcının kayıt olurken API'ye göndereceği verileri temsil eder.
public class RegisterDto
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    // Kullanıcının e-posta adresini tutar.
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
    // Kullanıcının şifresini tutar.
}
