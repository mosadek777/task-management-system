using TaskManagement.Api.DTOs;

namespace TaskManagement.Api.Services;

/// <summary>
/// All business logic for tasks. Controllers depend on this interface, never on
/// AppDbContext (Article IV).
/// </summary>
/// <remarks>
/// A missing task is an expected outcome, not an exceptional one (FR-026), so it
/// is signalled by null / false rather than by throwing. That lets the controller
/// write "return x is null ? NotFound() : Ok(x)" with no exception filter, no
/// custom exception type and no extra middleware.
/// </remarks>
public interface ITaskService
{
    /// <summary>All tasks, newest first, optionally narrowed by title text and/or status. [US-02, US-06, US-07]</summary>
    Task<IEnumerable<TaskResponseDto>> GetAllAsync(string? search, Models.TaskStatus? status);

    /// <summary>One task, or null when no task has that id. [US-03]</summary>
    Task<TaskResponseDto?> GetByIdAsync(int id);

    /// <summary>Creates a task and returns it with its server-assigned id and capture moment. [US-01]</summary>
    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto);

    /// <summary>Updates a task, or returns null when no task has that id. Never creates. [US-04]</summary>
    Task<TaskResponseDto?> UpdateAsync(int id, UpdateTaskDto dto);

    /// <summary>Permanently removes a task; false when no task has that id. [US-05]</summary>
    Task<bool> DeleteAsync(int id);
}
