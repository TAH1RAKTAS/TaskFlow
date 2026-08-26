using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
namespace TaskFlow.Services;
public class ReminderWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<ReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendDueReminders(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
    private async Task SendDueReminders(CancellationToken token)
    {
        var address = config["Gmail:Address"];
        var password = config["Gmail:AppPassword"];
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(password)) return;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var today = DateTime.UtcNow.Date;
        var reminders = await db.TaskReminders.Include(x => x.TaskItem).Where(x => !x.IsSent && x.TaskItem.DueDate != null && x.TaskItem.DueDate.Value.Date == today.AddDays(x.DaysBefore)).ToListAsync(token);
        foreach (var reminder in reminders)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587) { EnableSsl = true, Credentials = new NetworkCredential(address, password) };
                await client.SendMailAsync(new MailMessage(address, reminder.RecipientEmail, $"TaskFlow: {reminder.TaskItem.Title}", $"{reminder.TaskItem.Title} görevinin son tarihine {reminder.DaysBefore} gün kaldı."), token);
                reminder.IsSent = true;
            }
            catch (Exception exception) { logger.LogError(exception, "Hatırlatıcı gönderilemedi: {ReminderId}", reminder.Id); }
        }
        await db.SaveChangesAsync(token);
    }
}
