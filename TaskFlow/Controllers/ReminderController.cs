using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Models;
namespace TaskFlow.Controllers;
[ApiController, Route("reminders"), Authorize]
public class ReminderController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var reminders = await context.TaskReminders.Include(x => x.TaskItem).Where(x => x.UserId == userId)
            .Select(x => new { x.Id, TaskTitle = x.TaskItem.Title, x.RecipientEmail, x.DaysBefore, x.IsSent }).ToListAsync();
        return Ok(new ApiResponse<object>(true, "Hatırlatıcılar getirildi.", reminders));
    }
    [HttpPost]
    public async Task<IActionResult> Create(CreateReminderDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var task = await context.Tasks.AnyAsync(x => x.Id == dto.TaskItemId && x.UserId == userId && x.DueDate != null);
        if (!task) return BadRequest("Son tarihi olan kendi görevinizi seçin.");
        var exists = await context.TaskReminders.AnyAsync(x =>
            x.TaskItemId == dto.TaskItemId &&
            x.UserId == userId &&
            x.RecipientEmail == dto.RecipientEmail.Trim().ToLower() &&
            x.DaysBefore == dto.DaysBefore &&
            !x.IsSent);
        if (exists) return Conflict("Bu hatırlatıcı zaten mevcut.");
        context.TaskReminders.Add(new TaskReminder
        {
            TaskItemId = dto.TaskItemId,
            UserId = userId,
            RecipientEmail = dto.RecipientEmail.Trim().ToLowerInvariant(),
            DaysBefore = dto.DaysBefore
        });
        await context.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, "Hatırlatıcı kaydedildi."));
    }
}
