using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs;

/// <summary>
/// Output for every endpoint that returns a task. One shape serves both the list
/// and the detail view, so AC-03.2 ("complete detail") needs no second DTO.
/// </summary>
public class TaskResponseDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Priority Priority { get; set; }

    public Models.TaskStatus Status { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
