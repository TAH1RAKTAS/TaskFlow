using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;
// EF Core ve Model sınıflarını kullanabilmek için
namespace TaskFlow.Data;
// Data katmanı
public class ApplicationDbContext : DbContext
// Veritabanı işlemlerini ve bağlantısını yönetir
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    // DbContext ayarlarını üst sınıfa gönderir
    {
    }
    public DbSet<TaskItem> Tasks { get; set; }
    // TaskItem tablosunu temsil eder
    public DbSet<User> Users { get; set; }
    public DbSet<TaskReminder> TaskReminders { get; set; }
    //User tablosunu temsil eder
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(user => user.Email)
            .HasMaxLength(254);
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();
        modelBuilder.Entity<TaskItem>()
            .HasIndex(task => new { task.UserId, task.Title })
            .IsUnique();
    }
}
