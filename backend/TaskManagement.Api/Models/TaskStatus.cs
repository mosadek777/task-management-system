namespace TaskManagement.Api.Models;

/// <summary>
/// How far a task has progressed.
/// </summary>
/// <remarks>
/// This name deliberately shadows System.Threading.Tasks.TaskStatus within this
/// namespace. If a confusing conversion error mentions TaskStatus, qualify it as
/// TaskManagement.Api.Models.TaskStatus rather than renaming this type.
/// The "Todo" default comes from the DTO property initializers (see Priority).
/// </remarks>
public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}
