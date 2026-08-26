using TaskFlow.DTOs;

namespace TaskFlow.Services;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterDto registerDto);
    // Yeni kullanıcı kaydı oluşturur

    Task<string?> LoginAsync(LoginDto loginDto);
    // Kullanıcı bilgilerini kontrol eder ve başarılıysa JWT token döndürür
}