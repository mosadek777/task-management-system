namespace TaskManagement.Api.Models;

/// <summary>
/// How important a task is.
/// </summary>
/// <remarks>
/// Declaration order is deliberate and must NOT be changed: Low is the zero value.
/// The "Medium" default required by FR-004 / AC-01.11 comes from the property
/// initializers on CreateTaskDto and UpdateTaskDto, not from this ordering.
/// Reordering to make Medium zero would make the enum read wrongly forever to
/// solve a problem one initializer already solves.
/// </remarks>
public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2
}
