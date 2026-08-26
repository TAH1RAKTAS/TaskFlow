using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Exceptions;
using TaskFlow.Services;

namespace TaskFlow.Tests;

public class TaskServiceTests
{
    [Fact]
    public async Task CreateTaskAsync_AyniBaslikVarsa_BusinessExceptionFirlatmali()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TaskFlowTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var dto = new TaskItemDto
        {
            Title = "Test Task",
            Description = "Test Task açıklaması"
        };
        await service.CreateTaskAsync(dto, 1);
        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateTaskAsync(dto, 1));
        Assert.Equal("Bu başlıkta bir Task zaten mevcut.", exception.Message);
    }
    [Fact]
    public async Task CreateTaskAsync_GecerliTask_TaskOlusturmali()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("CreateTaskTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var dto = new TaskItemDto
        {
            Title = "Docker",
            Description = "Docker öğrenme görevini tamamla"
        };
        var result = await service.CreateTaskAsync(dto, 1);
        Assert.NotNull(result);
        Assert.Equal("Docker", result.Title);
        Assert.True(result.Id > 0);
    }
    [Fact]
    public async Task GetTaskByIdAsync_TaskVarsa_TaskDonmeli()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("GetTaskTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var dto = new TaskItemDto
        {
            Title = "Testing",
            Description = "Testing konusunu öğren"
        };
        var createdTask = await service.CreateTaskAsync(dto, 1);
        var result = await service.GetTaskByIdAsync(createdTask.Id, 1);
        Assert.NotNull(result);
        Assert.Equal(createdTask.Id, result.Id);
        Assert.Equal("Testing", result.Title);
    }
    [Fact]
    public async Task GetTaskByIdAsync_TaskYoksa_NullDonmeli()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("GetMissingTaskTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var result = await service.GetTaskByIdAsync(999, 1);
        Assert.Null(result);
    }
    [Fact]
    public async Task UpdateTaskAsync_TaskVarsa_Guncellemeli()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("UpdateTaskTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var createDto = new TaskItemDto
        {
            Title = "Eski Başlık",
            Description = "Eski Task açıklaması"
        };
        var createdTask = await service.CreateTaskAsync(createDto, 1);
        var updateDto = new TaskItemDto
        {
            Title = "Yeni Başlık",
            Description = "Yeni Task açıklaması"
        };
        var result = await service.UpdateTaskAsync(createdTask.Id, updateDto, 1);
        Assert.NotNull(result);
        Assert.Equal("Yeni Başlık", result.Title);
        Assert.Equal("Yeni Task açıklaması", result.Description);
    }
    [Fact]
    public async Task DeleteTaskAsync_TaskVarsa_Silmeli()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("DeleteTaskTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var dto = new TaskItemDto
        {
            Title = "Silinecek Task",
            Description = "Bu Task silme testi için oluşturuldu"
        };
        var createdTask = await service.CreateTaskAsync(dto, 1);
        var result = await service.DeleteTaskAsync(createdTask.Id, 1);
        var deletedTask = await context.Tasks.FindAsync(createdTask.Id);
        Assert.True(result);
        Assert.Null(deletedTask);
    }
    [Fact]
    public async Task GetTaskByIdAsync_BaskaKullanicininTaski_NullDonmeli()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("UserIsolationTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var dto = new TaskItemDto
        {
            Title = "Kullanıcı 1 Task",
            Description = "Kullanıcı izolasyonu için oluşturuldu"
        };
        var createdTask = await service.CreateTaskAsync(dto, 1);
        var result = await service.GetTaskByIdAsync(createdTask.Id, 2);
        Assert.Null(result);
    }
    [Fact]
    public async Task CreateTaskAsync_AciklamaBasliktanKisaysa_BusinessExceptionFirlatmali()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("ValidationTestDb")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<TaskService>>();
        var service = new TaskService(context, logger.Object);
        var dto = new TaskItemDto
        {
            Title = "Çok Uzun Bir Task Başlığı",
            Description = "Kısa"
        };
        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateTaskAsync(dto, 1));
        Assert.Equal(
            "Task açıklaması, başlıktan daha kısa olamaz.",
            exception.Message);
    }
}