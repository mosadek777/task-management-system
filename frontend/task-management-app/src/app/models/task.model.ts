/** Mirrors the API's string enums. The API returns "InProgress"; the UI shows "In Progress". */
export type Priority = 'Low' | 'Medium' | 'High';
export type TaskStatus = 'Todo' | 'InProgress' | 'Done';

export const PRIORITIES: Priority[] = ['Low', 'Medium', 'High'];
export const STATUSES: TaskStatus[] = ['Todo', 'InProgress', 'Done'];

/** Display labels — the wire format is never prettified server-side (AC-08.5, AC-13.9). */
export const STATUS_LABELS: Record<TaskStatus, string> = {
  Todo: 'To Do',
  InProgress: 'In Progress',
  Done: 'Done',
};

/** Mirrors TaskResponseDto. */
export interface Task {
  id: number;
  title: string;
  description: string | null;
  priority: Priority;
  status: TaskStatus;
  /** yyyy-MM-dd, or null. Deliberately a plain date string: no time, no timezone (AC-10.3). */
  dueDate: string | null;
  /** ISO 8601 UTC instant. */
  createdAt: string;
}

/** Mirrors CreateTaskDto / UpdateTaskDto — note the absence of id and createdAt. */
export interface TaskRequest {
  title: string;
  description: string | null;
  priority: Priority;
  status: TaskStatus;
  dueDate: string | null;
}

/** RFC 7807 ProblemDetails, as returned by [ApiController] on a 400. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  /** Per-field validation messages, keyed by property name. */
  errors?: Record<string, string[]>;
}
