namespace TaskFlow.Models;

public class User
{
    public int Id { get; set; }
    // Kullanıcının benzersiz ID'sini tutar.
    public string Email { get; set; } = string.Empty;
    // Kullanıcının e-posta adresini tutar.
    public string PasswordHash { get; set; } = string.Empty;
    // Kullanıcının şifresinin hashlenmiş halini tutar.
    public int DefaultPageSize { get; set; } = 5;
    public string DefaultSort { get; set; } = "due_date";
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    // Kullanıcının sahip olduğu Task'ları tutar.
}
