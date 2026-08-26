using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    // Kullanıcının giriş yapacağı email
    [Required(ErrorMessage = "Şifre zorunludur.")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
    // Kullanıcının giriş yapacağı şifre
}
