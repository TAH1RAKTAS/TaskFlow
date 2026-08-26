namespace TaskFlow.Models;
public class TaskReminder
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public int UserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public int DaysBefore { get; set; }
    public bool IsSent { get; set; }
}
