using System.IdentityModel.Tokens.Jwt;
// JWT token oluşturmak için.
using System.Security.Claims;
// Token içine kullanıcı bilgilerini eklemek için.
using System.Text;
// Gizli anahtarı byte dizisine çevirmek için.
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
// JWT imzalama işlemleri için.
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Models;

namespace TaskFlow.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    // DbContext ve JWT ayarlarına erişmek için kullanılır.
    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    // Gerekli bağımlılıkları Service'e verir.
    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        var normalizedEmail = registerDto.Email.Trim().ToLowerInvariant();
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        // Email'in daha önce kullanılıp kullanılmadığını kontrol eder.
        if (existingUser != null)
        {
            return false;
        }
        // Email zaten kayıtlıysa kayıt işlemini durdurur.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
        // Kullanıcının şifresini hashler.
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHash
        };
        // Hashlenmiş şifreyle yeni kullanıcı oluşturur.
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // Kullanıcıyı veritabanına kaydeder.
        return true;
        // Kayıt işleminin başarılı olduğunu bildirir.
    }

    public async Task<string?> LoginAsync(LoginDto loginDto)
    {
        var normalizedEmail = loginDto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        // Email adresine göre kullanıcıyı veritabanında bulur.
        if (user == null)
        {
            return null;
        }
        // Kullanıcı bulunamazsa login işlemini başarısız yapar.
        var passwordValid = BCrypt.Net.BCrypt.Verify(
            loginDto.Password,
            user.PasswordHash);
        // Girilen şifreyi database'deki hashlenmiş şifreyle karşılaştırır.
        if (!passwordValid)
        {
            return null;
        }
        // Şifre yanlışsa login işlemini başarısız yapar.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        // appsettings.json içindeki JWT gizli anahtarını alır.
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);
        // JWT'nin HMAC-SHA256 algoritmasıyla imzalanmasını sağlar.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };
        // JWT içine kullanıcının Id ve Email bilgilerini ekler.
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);
        // 2 saat geçerli olacak JWT token oluşturur.
        return new JwtSecurityTokenHandler().WriteToken(token);
        // JWT tokenını string formatına çevirip döndürür.
    }
}
