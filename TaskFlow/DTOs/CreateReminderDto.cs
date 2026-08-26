using System.ComponentModel.DataAnnotations;
namespace TaskFlow.DTOs;
public class CreateReminderDto
{
    [Range(1, int.MaxValue)] public int TaskItemId { get; set; }
    [Required, EmailAddress, MaxLength(254)] public string RecipientEmail { get; set; } = string.Empty;
    [Range(0, 365)] public int DaysBefore { get; set; }
}
