using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Controllers;

/// <summary>
/// Thin by constitutional requirement (Article IV): every action binds the request,
/// calls one service method, and maps the result to a status code. No business
/// rules, no LINQ, no DbContext — and no TaskItem in any signature (Article V).
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>All tasks, newest first. Optionally narrowed by title text and/or status. [US-02, US-06, US-07]</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Models.TaskStatus? status)
    {
        var tasks = await _taskService.GetAllAsync(search, status);
        return Ok(tasks);   // an empty list is a valid 200, never a 404 (AC-02.4)
    }

    /// <summary>One task by id. [US-03]</summary>
    /// <remarks>
    /// The route deliberately carries no :int constraint. With one, /api/tasks/abc
    /// would fail to match and return 404; without one, model binding fails and
    /// [ApiController] returns 400 — which is what AC-03.6 requires.
    /// </remarks>
    [HttpGet("{id}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> GetById(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        return task is null ? NotFound() : Ok(task);
    }

    /// <summary>Creates a task. [US-01]</summary>
    /// <remarks>
    /// Validation needs no code here: [ApiController] returns 400 with a
    /// ProblemDetails body before this method body runs if any Data Annotation
    /// on CreateTaskDto fails (AC-01.14).
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponseDto>> Create([FromBody] CreateTaskDto dto)
    {
        var created = await _taskService.CreateAsync(dto);

        // CreatedAtRoute produces the 201 plus the Location header (AC-01.3).
        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a task. Never creates one. [US-04]</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> Update(int id, [FromBody] UpdateTaskDto dto)
    {
        var updated = await _taskService.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);   // AC-04.9
    }

    /// <summary>Permanently deletes a task. [US-05]</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _taskService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
