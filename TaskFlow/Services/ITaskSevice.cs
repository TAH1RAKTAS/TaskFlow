using TaskFlow.DTOs;

namespace TaskFlow.Services;

public interface ITaskService
{
    Task<PagedResult<TaskItemDto>> GetTasksAsync(int userId, int page, int pageSize, string? search, string? sort, string? status);
    // Kullanıcının belirtilen sayfadaki Task'larını getirir.
    Task<TaskItemDto?> GetTaskByIdAsync(int id, int userId);
    // Kullanıcıya ait belirli bir Task'ı ID ile getirir.
    Task<TaskItemDto> CreateTaskAsync(TaskItemDto taskDto, int userId);
    // Kullanıcıya ait yeni bir Task oluşturur.
    Task<TaskItemDto?> UpdateTaskAsync(int id, TaskItemDto taskDto, int userId);
    // Kullanıcıya ait mevcut bir Task'ı günceller.
    Task<bool> UpdateTaskStatusAsync(int id, string status, int userId);
    // Kullanıcıya ait Task'ın durumunu günceller.
    Task<bool> DeleteTaskAsync(int id, int userId);
    // Kullanıcıya ait Task'ı siler ve işlemin başarılı olup olmadığını döndürür.
}
