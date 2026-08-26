namespace TaskFlow.Models;
public class TaskItem
{
    public int Id { get; set; }
    // Task'ın benzersiz ID'sini tutar.
    public string Title { get; set; } = string.Empty;
    // Task'ın başlığını tutar.
    public string Description { get; set; } = string.Empty;
    // Task'ın açıklamasını tutar.
    public string Priority { get; set; } = "Orta";
    // Task'ın öncelik seviyesini tutar.
    public string Status { get; set; } = "Başlamadı";
    // Task'ın ilerleme durumunu tutar.
    public DateTime? DueDate { get; set; }
    // Task'ın son tarihini tutar.
    public int UserId { get; set; }
    // Task'ın hangi kullanıcıya ait olduğunu belirtir.
    public User User { get; set; } = null!;
    // Task ile User arasında navigation property ilişkisi kurar.
}
