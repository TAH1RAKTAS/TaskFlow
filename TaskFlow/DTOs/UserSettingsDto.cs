using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs;

public sealed class UserSettingsDto
{
    [Range(5, 50)]
    public int DefaultPageSize { get; set; } = 5;
    [RegularExpression("^(due_date|due_date_desc|title|title_desc|priority|priority_desc|status)$")]
    public string DefaultSort { get; set; } = "due_date";
}
