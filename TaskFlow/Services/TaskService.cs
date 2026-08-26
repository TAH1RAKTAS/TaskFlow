using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Exceptions;
using TaskFlow.Models;
namespace TaskFlow.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    // Veritabanına erişmek için DbContext'i tutar.
    private readonly ILogger<TaskService> _logger;
    public TaskService(ApplicationDbContext context,ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }
    // DbContext'i Dependency Injection ile Service'e verir.
    public async Task<PagedResult<TaskItemDto>> GetTasksAsync(int userId, int page, int pageSize, string? search, string? sort, string? status)
    {
        _logger.LogInformation(

            "Kullanıcı {UserId} görevlerini getiriyor. Sayfa: {Page}, Sayfa Boyutu: {PageSize}, Arama: {Search}, Sıralama: {Sort}",
            userId, page, pageSize, search, sort);
        var query = _context.Tasks.Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        if (status == "active")
            query = query.Where(x => x.Status != "Tamamlandı");
        else if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (sort == "title")
            query = query.OrderBy(x => x.Title);
        else if (sort == "title_desc")
            query = query.OrderByDescending(x => x.Title);
        else if (sort == "priority")
            query = query.OrderByDescending(x => x.Priority == "Yüksek" ? 3 : x.Priority == "Orta" ? 2 : 1);
        else if (sort == "priority_desc")
            query = query.OrderBy(x => x.Priority == "Yüksek" ? 3 : x.Priority == "Orta" ? 2 : 1);
        else if (sort == "due_date")
            query = query.OrderBy(x => x.DueDate == null).ThenBy(x => x.DueDate);
        else if (sort == "due_date_desc")
            query = query.OrderByDescending(x => x.DueDate);
        else if (sort == "status")
            query = query.OrderBy(x => x.Status == "Tamamlandı" ? 3 : x.Status == "Devam Ediyor" ? 2 : 1);
        else
            query = query.OrderBy(x => x.DueDate == null).ThenBy(x => x.DueDate).ThenBy(x => x.Id);
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TaskItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Priority = x.Priority,
                Status = x.Status,
                DueDate = x.DueDate
            })
            .ToListAsync();
        return new PagedResult<TaskItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    // Kullanıcının kendi Task'larını getirir ve DTO'ya dönüştürür.
    public async Task<TaskItemDto?> GetTaskByIdAsync(int id, int userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        // ID ve UserId eşleşen Task'ı bulur.
        if (task == null)
            return null;
        // Task bulunamazsa null döndürür.
        return MapToDto(task);
        // Entity'yi DTO'ya çevirip döndürür.
    }
    public async Task<TaskItemDto> CreateTaskAsync(TaskItemDto taskDto, int userId)
    {
        _logger.LogInformation(
            "Kullanıcı {UserId} yeni bir Task oluşturuyor. Başlık: {Title}",
            userId,
            taskDto.Title);
        if (string.IsNullOrWhiteSpace(taskDto.Title))
        {
            _logger.LogWarning(
                "Kullanıcı {UserId} boş başlıkla Task oluşturmaya çalıştı.",
                userId);
            throw new BusinessException("Görev başlığı boş olamaz.");
        }
        var exists = await _context.Tasks
            .AnyAsync(x => x.Title == taskDto.Title && x.UserId == userId);
        // Aynı kullanıcının aynı başlıkta Task'ı olup olmadığını kontrol eder.
        if (exists)
        {
            _logger.LogWarning(
                "Kullanıcı {UserId}, zaten mevcut olan {Title} başlıklı Task'ı oluşturmaya çalıştı.",
                userId,
                taskDto.Title);
            throw new BusinessException("Bu başlıkta bir Task zaten mevcut.");
        }
        // Aynı başlık varsa BusinessException fırlatır.
        ValidateTaskRules(taskDto);
        // Task'ın iş kurallarına uygunluğunu kontrol eder.
        var task = new TaskItem
        {
            Title = taskDto.Title,
            Description = taskDto.Description,
            Priority = taskDto.Priority,
            Status = taskDto.Status,
            DueDate = taskDto.DueDate,
            UserId = userId
        };
        // DTO'dan yeni Task entity'si oluşturur.
        _context.Tasks.Add(task);
        // Task'ı EF Core'a ekler.
        await _context.SaveChangesAsync();
        // Task'ı veritabanına kaydeder.
        taskDto.Id = task.Id;
        // Database'in oluşturduğu ID'yi DTO'ya aktarır.
        _logger.LogInformation(
            "Kullanıcı {UserId} için {TaskId} numaralı Task başarıyla oluşturuldu.",
            userId,
            task.Id);
        return taskDto;
        // Oluşturulan Task'ı döndürür.
    }
    public async Task<TaskItemDto?> UpdateTaskAsync(int id, TaskItemDto taskDto, int userId)
    {
        _logger.LogInformation("Kullanıcı {UserId}, {TaskId} numaralı Task'ı güncelliyor.", userId, id);
        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        // ID ve UserId eşleşen Task'ı bulur.
        if (task == null)
        {
            _logger.LogWarning("Kullanıcı {UserId} için {TaskId} numaralı Task bulunamadı.",
                userId, id);
            return null; 
        }
        // Task bulunamazsa null döndürür.
        var exists = await _context.Tasks
            .AnyAsync(x => x.Title == taskDto.Title && x.Id != id && x.UserId == userId);
        // Aynı başlığın başka bir Task'ta kullanılıp kullanılmadığını kontrol eder.
        if (exists)
        {_logger.LogWarning(

                "Kullanıcı {UserId}, {TaskId} numaralı Task'ı mevcut olan {Title} başlığıyla güncellemeye çalıştı.",
                userId, id, taskDto.Title);
            throw new BusinessException("Bu başlıkta başka bir Task zaten mevcut.");
            // Aynı başlık varsa BusinessException fırlatır.
        }
        ValidateTaskRules(taskDto);
        // Güncellenen Task'ın kurallarını kontrol eder.
        task.Title = taskDto.Title;
        task.Description = taskDto.Description;
        // Task'ın başlık ve açıklamasını günceller.
        task.Priority = taskDto.Priority;
        task.Status = taskDto.Status;
        task.DueDate = taskDto.DueDate;
        await _context.SaveChangesAsync();
        // Güncellemeyi veritabanına kaydeder.
        taskDto.Id = task.Id;
        // Task ID'sini DTO'ya aktarır.
        _logger.LogInformation(
            "Kullanıcı {UserId}, {TaskId} numaralı Task'ı başarıyla güncelledi.",
            userId, id);
        return taskDto;
        // Güncellenmiş Task'ı döndürür.
    }
    public async Task<bool> DeleteTaskAsync(int id, int userId)
    {
        _logger.LogInformation("Kullanıcı {UserId}, {TaskId} numaralı Task'ı silmeye çalışıyor.",
            userId, id);
        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        // ID ve UserId eşleşen Task'ı bulur.
        if (task == null)
        {
            _logger.LogWarning("Kullanıcı {UserId} için {TaskId} numaralı Task bulunamadı.",
                userId, id);
            return false;
            // Task bulunamazsa false döndürür.
        }
        _context.Tasks.Remove(task);
        // Task'ı silinmek üzere işaretler.
        await _context.SaveChangesAsync();
        // Silme işlemini veritabanına kaydeder.
        _logger.LogInformation("Kullanıcı {UserId}, {TaskId} numaralı Task'ı başarıyla sildi.",
            userId, id);
        return true;
        // Silme işleminin başarılı olduğunu bildirir.
    }
    public async Task<bool> UpdateTaskStatusAsync(int id, string status, int userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (task == null)
            return false;

        task.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }
    private void ValidateTaskRules(TaskItemDto taskDto)
    {
        if (taskDto.Description.Length < taskDto.Title.Length)
        {
            throw new BusinessException("Task açıklaması, başlıktan daha kısa olamaz.");
        }
        // Açıklamanın başlıktan kısa olmasını engeller.
    }
    private TaskItemDto MapToDto(TaskItem task)
    {
        return new TaskItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority
            ,Status = task.Status
            ,DueDate = task.DueDate
        };
        // Task entity'sini DTO'ya dönüştürür.
    }
}
