using System.ComponentModel.DataAnnotations;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs;

/// <summary>
/// Input for PUT /api/tasks/{id} [US-04]. Rules are identical to CreateTaskDto
/// because AC-04.7 requires them to match identically.
/// </summary>
/// <remarks>
/// Kept as a separate class from CreateTaskDto even though it is currently
/// identical: they are contracts for two different operations that are free to
/// diverge, and sharing one class would make Swagger show the same schema name
/// for both. No Id property — the route id is authoritative (AC-04.3).
/// </remarks>
public class UpdateTaskDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
    public string? Description { get; set; }

    [EnumDataType(typeof(Priority), ErrorMessage = "Priority must be Low, Medium or High.")]
    public Priority Priority { get; set; } = Priority.Medium;

    [EnumDataType(typeof(Models.TaskStatus), ErrorMessage = "Status must be Todo, InProgress or Done.")]
    public Models.TaskStatus Status { get; set; } = Models.TaskStatus.Todo;

    public DateOnly? DueDate { get; set; }
}
