using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.DTOs;

namespace TaskFlow.Controllers;

[ApiController]
[Route("settings")]
[Authorize]
public class SettingsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> Get()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var settings = await context.Users.Where(user => user.Id == userId)
            .Select(user => new UserSettingsDto { DefaultPageSize = user.DefaultPageSize, DefaultSort = user.DefaultSort })
            .SingleAsync();
        return Ok(new ApiResponse<UserSettingsDto>(true, "Ayarlar getirildi.", settings));
    }

    [HttpPut]
    public async Task<IActionResult> Update(UserSettingsDto settings)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await context.Users.FindAsync(userId);
        if (user is null) return NotFound();
        user.DefaultPageSize = settings.DefaultPageSize;
        user.DefaultSort = settings.DefaultSort;
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<UserSettingsDto>(true, "Ayarlar kaydedildi.", settings));
    }
}
