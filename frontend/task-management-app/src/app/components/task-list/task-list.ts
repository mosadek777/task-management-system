import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { TaskService } from '../../services/task.service';
import { ConfirmDialog } from '../confirm-dialog/confirm-dialog';
import { environment } from '../../../environments/environment';
import {
  STATUSES,
  STATUS_LABELS,
  Task,
  TaskStatus,
} from '../../models/task.model';

type StatusFilter = TaskStatus | 'All';

/**
 * [US-08] the list · [US-12] search · [US-13] status filter · [US-11] delete
 *
 * Search and filter are held in ONE piece of state and always sent together on the
 * same request. That is what makes them combine (AC-13.5) and what stops either
 * one clearing the other (AC-13.6).
 */
@Component({
  selector: 'app-task-list',
  imports: [ReactiveFormsModule, RouterLink, ConfirmDialog],
  templateUrl: './task-list.html',
})
export class TaskList implements OnInit {
  private readonly taskService = inject(TaskService);

  readonly tasks = signal<Task[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly searchControl = new FormControl('');
  readonly statusFilter = signal<StatusFilter>('All');

  readonly taskPendingDelete = signal<Task | null>(null);
  readonly deleting = signal(false);

  readonly statuses = STATUSES;
  readonly statusLabels = STATUS_LABELS;

  ngOnInit(): void {
    // AC-12.8: debounced, so typing does not fire one request per keystroke.
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => this.load());

    this.load();
  }

  /** The single place the list is fetched. Everything re-runs this (AC-08.10). */
  load(): void {
    this.loading.set(true);
    this.error.set(null);

    const status = this.statusFilter() === 'All' ? null : (this.statusFilter() as TaskStatus);

    this.taskService.getAll(this.searchControl.value, status).subscribe({
      next: (tasks) => {
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: () => {
        // AC-08.8 / AC-12.10: readable message, previous results left on screen.
        // The address is read from config rather than hardcoded, so this message
        // can never name a port the app is not actually calling (plan.md 2.1).
        this.error.set(`Could not reach the API at ${environment.apiBaseUrl}. Is the backend running?`);
        this.loading.set(false);
      },
    });
  }

  setStatusFilter(status: StatusFilter): void {
    this.statusFilter.set(status);
    this.load(); // the search term is untouched (AC-13.6)
  }

  clearSearch(): void {
    this.searchControl.setValue(''); // triggers load via valueChanges (AC-12.6)
  }

  askDelete(task: Task): void {
    this.taskPendingDelete.set(task); // nothing is sent yet (AC-11.2)
  }

  cancelDelete(): void {
    this.taskPendingDelete.set(null); // AC-11.5
  }

  confirmDelete(): void {
    const task = this.taskPendingDelete();
    if (!task) return;

    this.deleting.set(true);
    this.taskService.delete(task.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.taskPendingDelete.set(null);
        this.load(); // AC-11.6
      },
      error: (err) => {
        this.deleting.set(false);
        this.taskPendingDelete.set(null);
        if (err?.status === 404) {
          // AC-11.7: already deleted elsewhere — resync rather than fail.
          this.error.set('That task no longer exists. The list has been refreshed.');
          this.load();
        } else {
          // AC-11.8: the row stays; display never disagrees with what is stored.
          this.error.set('Could not delete the task. It is still in your list.');
        }
      },
    });
  }

  /** AC-08.3: never a blank gap, never the literal word "null". */
  formatDueDate(dueDate: string | null): string {
    if (!dueDate) return 'No due date';
    const [year, month, day] = dueDate.split('-').map(Number);
    return new Date(year, month - 1, day).toLocaleDateString(undefined, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }

  priorityClass(task: Task): string {
    switch (task.priority) {
      case 'High':
        return 'bg-red-100 text-red-800 ring-red-200';
      case 'Medium':
        return 'bg-amber-100 text-amber-800 ring-amber-200';
      default:
        return 'bg-slate-100 text-slate-700 ring-slate-200';
    }
  }

  statusClass(task: Task): string {
    switch (task.status) {
      case 'Done':
        return 'bg-green-100 text-green-800 ring-green-200';
      case 'InProgress':
        return 'bg-blue-100 text-blue-800 ring-blue-200';
      default:
        return 'bg-slate-100 text-slate-700 ring-slate-200';
    }
  }

  /** True when the list is empty because of narrowing rather than having no tasks. */
  get isNarrowed(): boolean {
    return !!this.searchControl.value?.trim() || this.statusFilter() !== 'All';
  }

  /** The active filter's display label. A template cannot narrow 'All' out of the union. */
  activeStatusLabel(): string {
    const current = this.statusFilter();
    return current === 'All' ? 'All' : this.statusLabels[current];
  }
}
