using System.ComponentModel.DataAnnotations;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs;

/// <summary>
/// Input for POST /api/tasks [US-01].
/// </summary>
/// <remarks>
/// Deliberately has no Id and no CreatedAt property. That absence IS the
/// over-posting defence (AC-01.4, AC-01.5): a client can post
/// {"id": 99, "createdAt": "1999-01-01"} and model binding has nowhere to put
/// those values. No code checks for them, because no code needs to.
/// </remarks>
public class CreateTaskDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
    public string? Description { get; set; }

    // The initializer is load-bearing, not cosmetic. Priority's zero value is Low,
    // so without "= Priority.Medium" an omitted priority would deserialise to Low
    // and FR-004 / AC-01.11 would silently fail.
    [EnumDataType(typeof(Priority), ErrorMessage = "Priority must be Low, Medium or High.")]
    public Priority Priority { get; set; } = Priority.Medium;

    [EnumDataType(typeof(Models.TaskStatus), ErrorMessage = "Status must be Todo, InProgress or Done.")]
    public Models.TaskStatus Status { get; set; } = Models.TaskStatus.Todo;

    // No validation attribute, deliberately: FR-028 accepts any date including the
    // past, and no date-range rule exists anywhere in this system.
    public DateOnly? DueDate { get; set; }
}
