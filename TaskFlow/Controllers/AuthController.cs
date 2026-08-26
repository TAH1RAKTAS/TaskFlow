using Microsoft.AspNetCore.Mvc;
using TaskFlow.DTOs;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    // Auth işlemlerini Service katmanına yönlendirmek için kullanılır.
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    // IAuthService'i Dependency Injection ile Controller'a verir.

    [HttpPost("register")]
    // POST /Auth/register isteğini karşılar.
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);
        // Register işlemini Service katmanına gönderir.
        if (!result)
            return BadRequest("Email zaten kullanılıyor.");
        // Email zaten kayıtlıysa 400 Bad Request döndürür.
        return Ok("Kullanıcı başarıyla oluşturuldu.");
        // Kullanıcı başarıyla oluşturulduysa 200 OK döndürür.
    }

    [HttpPost("login")]
    // POST /Auth/login isteğini karşılar.
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        // Login işlemini Service katmanına gönderir.
        if (result == null)
            return Unauthorized("Email veya şifre hatalı.");
        // Kullanıcı bulunamazsa veya şifre yanlışsa 401 döndürür.
        return Ok(result);
        // Login başarılıysa JWT tokenını 200 OK ile döndürür.
    }
}