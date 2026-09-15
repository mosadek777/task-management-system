using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services;

/// <summary>
/// The only place business logic lives. Uses AppDbContext directly — no repository
/// layer, because DbContext is already a Unit of Work and DbSet is already a
/// repository (Article III).
/// </summary>
public class TaskService : ITaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    // [US-02, US-06, US-07] One method, both narrowings. Because search and status
    // are two optional Where clauses over the same query object, all four
    // combinations (neither / search / status / both) work with no extra code path.
    // That is FR-020 / AC-07.5 falling out of the design rather than being handled.
    public async Task<IEnumerable<TaskResponseDto>> GetAllAsync(string? search, Models.TaskStatus? status)
    {
        // AsNoTracking: reads never write, so change tracking would be wasted work.
        IQueryable<TaskItem> query = _context.Tasks.AsNoTracking();

        // [US-06] Whitespace-only is treated as no search at all (AC-06.7).
        // No ToLower(): SQL Server's default collation is already case-insensitive
        // (AC-06.3), and lowering both sides can prevent index use.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Title.Contains(term));
        }

        // [US-07]
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        // Nothing has executed yet. The whole query — filters, ordering and
        // projection — is translated to SQL and runs once, here (AC-06.9, AC-07.8).
        //
        // The projection is written inline rather than calling MapToResponse:
        // EF Core translates this expression tree into SQL and cannot translate an
        // arbitrary method call, so calling the helper here would fail at runtime
        // with "could not be translated". MapToResponse is used only on entities
        // already materialised in memory.
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    // [US-03]
    public async Task<TaskResponseDto?> GetByIdAsync(int id)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return task is null ? null : MapToResponse(task);
    }

    // [US-01]
    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title.Trim(),               // AC-01.7
            Description = NormaliseDescription(dto.Description),
            Priority = dto.Priority,
            Status = dto.Status,
            DueDate = dto.DueDate,                  // any date, including the past (AC-01.13)
            CreatedAt = DateTime.UtcNow             // server-assigned only (AC-01.5)
                                                    // Id is left for the database (AC-01.4)
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return MapToResponse(task);
    }

    // [US-04]
    public async Task<TaskResponseDto?> UpdateAsync(int id, UpdateTaskDto dto)
    {
        // Tracked deliberately (no AsNoTracking): change tracking is exactly what
        // we want here, so assigning properties is enough to produce the UPDATE.
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

        if (task is null)
        {
            return null;    // AC-04.9 — a PUT to a missing id never creates a task
        }

        task.Title = dto.Title.Trim();
        task.Description = NormaliseDescription(dto.Description);
        task.Priority = dto.Priority;
        task.Status = dto.Status;
        task.DueDate = dto.DueDate;
        // Id and CreatedAt are deliberately not assigned (AC-04.3, AC-04.4).

        await _context.SaveChangesAsync();

        return MapToResponse(task);
    }

    // [US-05] Physically removes the row. No flag, no archive, no restore (FR-030).
    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

        if (task is null)
        {
            return false;   // AC-05.4
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Hand-written mapping, no AutoMapper (Article III). This is where Id and
    /// CreatedAt are deliberately NOT copied from input.
    /// </summary>
    private static TaskResponseDto MapToResponse(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Priority = task.Priority,
        Status = task.Status,
        DueDate = task.DueDate,
        CreatedAt = task.CreatedAt
    };

    /// <summary>
    /// An empty or whitespace-only description means "no description" (FR-013), so
    /// it is stored as null rather than as "" — keeping one representation of absent.
    /// </summary>
    private static string? NormaliseDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
