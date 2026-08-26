using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlow.DTOs;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
// Sadece JWT ile giriş yapan kullanıcıların Controller'a erişmesini sağlar.
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    public TaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }
    // ITaskService'i Dependency Injection ile Controller'a bağlar.

    [HttpGet]
    public async Task<IActionResult> GetTasks(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? sort = null,
        string? status = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        if (page < 1)
            return BadRequest("Page 1'den küçük olamaz.");
        if (pageSize < 1 || pageSize > 50)
            return BadRequest("PageSize 1 ile 50 arasında olmalıdır.");
        var tasks = await _taskService.GetTasksAsync(
            int.Parse(userId), page, pageSize, search, sort, status);
        return Ok(new ApiResponse<PagedResult<TaskItemDto>>(
            true,
            "Task'lar başarıyla getirildi.",
            tasks));
    }
    [HttpGet("{id}")]
    // GET /Task/5 gibi ID'ye göre görev getirir.
    public async Task<IActionResult> GetTask(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // JWT içinden kullanıcı ID'sini alır.
        if (userId == null)
            return Unauthorized();
        // Kullanıcı ID'si yoksa 401 döndürür.
        var task = await _taskService.GetTaskByIdAsync(id, int.Parse(userId));
        // Görev ID'si ve kullanıcı ID'sini Service'e gönderir.
        if (task == null)
            return NotFound();
        // Görev bulunamazsa 404 döndürür.
        return Ok(new ApiResponse<TaskItemDto>(
            true,
            "Task başarıyla getirildi.",
            task));
    }

    [HttpPost]
    // POST /Task ile yeni görev oluşturur.
    public async Task<IActionResult> CreateTask(TaskItemDto taskDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // JWT içinden kullanıcı ID'sini alır.
        if (userId == null)
            return Unauthorized();
        // Kullanıcı ID'si yoksa 401 döndürür.
        var task = await _taskService.CreateTaskAsync(taskDto, int.Parse(userId));
        // DTO ve kullanıcı ID'sini Service'e gönderir.
        return CreatedAtAction(
            nameof(GetTask),
            new { id = task.Id },
            new ApiResponse<TaskItemDto>(
                true,
                "Task başarıyla oluşturuldu.",
                task));
    }

    [HttpPut("{id}")]
    // PUT /Task/5 ile mevcut görevi günceller.
    public async Task<IActionResult> UpdateTask(int id, TaskItemDto taskDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // JWT içinden kullanıcı ID'sini alır.
        if (userId == null)
            return Unauthorized();
        // Kullanıcı ID'si yoksa 401 döndürür.
        var task = await _taskService.UpdateTaskAsync(id, taskDto, int.Parse(userId));
        // Görev ID'si, DTO ve kullanıcı ID'sini Service'e gönderir.
        if (task == null)
            return NotFound();
        // Görev bulunamazsa 404 döndürür.
        return Ok(new ApiResponse<TaskItemDto>(
            true,
            "Task başarıyla güncellendi.",
            task));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto statusDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var updated = await _taskService.UpdateTaskStatusAsync(id, statusDto.Status, int.Parse(userId));
        if (!updated)
            return NotFound();

        return Ok(new ApiResponse<object>(true, "Görev durumu güncellendi.", null));
    }

    [HttpDelete("{id}")]
    // DELETE /Task/5 ile görevi siler.
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // JWT içinden kullanıcı ID'sini alır.
        if (userId == null)
            return Unauthorized();
        // Kullanıcı ID'si yoksa 401 döndürür.
        var deleted = await _taskService.DeleteTaskAsync(id, int.Parse(userId));
        // Görev ID'si ve kullanıcı ID'sini Service'e gönderir.
        if (!deleted)
            return NotFound();
        // Silinecek görev bulunamazsa 404 döndürür.
        return Ok(new ApiResponse<Object>(
        true,
        "Task başarıyla silindi."));
        // Silme sonucunu standart response formatında döndürür.
    }
}
