namespace TaskManagement.Api.Models;

/// <summary>
/// A single unit of work. This is the EF Core entity and MUST NEVER leave the
/// service layer — controllers accept and return DTOs only (Article V).
/// </summary>
/// <remarks>
/// Named TaskItem rather than Task so it does not collide with the async
/// System.Threading.Tasks.Task used by every method signature in this project.
/// There is deliberately no IsDeleted flag: deletion removes the row (FR-030).
/// </remarks>
public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Priority Priority { get; set; }

    public TaskStatus Status { get; set; }

    /// <summary>
    /// DateOnly, not DateTime: a due date has no time and no timezone. Using
    /// DateTime here is how the 20th gets stored and the 19th comes back after a
    /// UTC conversion — the exact bug AC-10.3 forbids.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Set by TaskService at creation and never changed afterwards (AC-01.5, AC-04.4).
    /// Never bound from a DTO — neither input DTO has this property.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
