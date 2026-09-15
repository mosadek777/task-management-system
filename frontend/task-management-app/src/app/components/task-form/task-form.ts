import { Component, OnInit, inject, signal, computed } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TaskService } from '../../services/task.service';
import {
  PRIORITIES,
  STATUSES,
  STATUS_LABELS,
  ProblemDetails,
  Priority,
  TaskRequest,
  TaskStatus,
} from '../../models/task.model';

/** Rejects a title that is only whitespace — [Required] alone would let "   " through. */
function notBlank(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string | null;
  if (value !== null && value !== undefined && value.trim().length === 0 && value.length > 0) {
    return { blank: true };
  }
  return null;
}

/**
 * [US-09] create · [US-10] edit — ONE component, two modes, switching on the route id.
 *
 * Sharing it is deliberate: AC-10.10 requires the edit rules to match the create
 * rules identically, and two components is exactly how those rule sets drift apart.
 */
@Component({
  selector: 'app-task-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './task-form.html',
})
export class TaskForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly taskService = inject(TaskService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly taskId = signal<number | null>(null);
  readonly isEditMode = computed(() => this.taskId() !== null);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly serverError = signal<string | null>(null);

  readonly priorities = PRIORITIES;
  readonly statuses = STATUSES;
  readonly statusLabels = STATUS_LABELS;

  // Mirrors the server's Data Annotations exactly (AC-09.7..AC-09.9, AC-10.10).
  // The server's copy is still the one that counts (FR-024).
  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200), notBlank]],
    description: ['', [Validators.maxLength(1000)]],
    // Typed as the full union, not the literal: "as const" would narrow the control
    // to exactly "Medium" and reject every other value when editing a task.
    priority: ['Medium' as Priority],  // AC-09.3
    status: ['Todo' as TaskStatus],    // AC-09.3
    dueDate: [''],                     // no date restriction at all (AC-09.12)
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) return;

    const id = Number(idParam);
    this.taskId.set(id);
    this.loading.set(true);

    this.taskService.getById(id).subscribe({
      next: (task) => {
        this.form.patchValue({
          title: task.title,
          // AC-10.2: absent renders as genuinely empty, never "null"
          description: task.description ?? '',
          priority: task.priority,
          status: task.status,
          // AC-10.3: the API sends yyyy-MM-dd and the date input wants yyyy-MM-dd.
          // Passing it straight through means no Date object is ever constructed,
          // so there is no timezone conversion and no day-shift.
          dueDate: task.dueDate ?? '',
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        // AC-10.8: a task that no longer exists says so, on a usable screen.
        this.loadError.set(
          err?.status === 404
            ? 'That task no longer exists. It may have been deleted.'
            : 'Could not load the task. Is the backend running?',
        );
      },
    });
  }

  save(): void {
    this.serverError.set(null);

    // AC-09.11: on submit, reveal every error at once.
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const raw = this.form.getRawValue();
    const payload: TaskRequest = {
      title: raw.title.trim(),
      description: raw.description.trim() ? raw.description.trim() : null,  // AC-10.7
      priority: raw.priority,
      status: raw.status,
      dueDate: raw.dueDate ? raw.dueDate : null,                            // AC-10.7
    };

    this.saving.set(true);  // AC-09.6: blocks a double submit

    const request$ = this.isEditMode()
      ? this.taskService.update(this.taskId()!, payload)
      : this.taskService.create(payload);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/']);  // AC-09.5, AC-10.5
      },
      error: (err) => {
        this.saving.set(false);
        this.applyServerErrors(err);
      },
    });
  }

  /**
   * AC-09.13 / AC-10.12: show the server's rejection against the right field and
   * KEEP everything the person typed. The form is never reset on failure.
   */
  private applyServerErrors(err: unknown): void {
    const response = err as { status?: number; error?: ProblemDetails };

    if (response?.status === 404) {
      this.serverError.set('That task no longer exists. It may have been deleted.');
      return;
    }

    const problem = response?.error;
    if (problem?.errors) {
      const unmatched: string[] = [];

      for (const [field, messages] of Object.entries(problem.errors)) {
        // ASP.NET Core returns PascalCase keys ("Title"); controls are camelCase.
        const controlName = field.charAt(0).toLowerCase() + field.slice(1);
        const control = this.form.get(controlName);
        if (control) {
          control.setErrors({ ...(control.errors ?? {}), server: messages.join(' ') });
          control.markAsTouched();
        } else {
          unmatched.push(...messages);
        }
      }

      if (unmatched.length > 0) {
        this.serverError.set(unmatched.join(' '));
      }
      return;
    }

    this.serverError.set(
      problem?.title ?? 'Could not save the task. Please check the values and try again.',
    );
  }

  /** The message to show under a field, if any. */
  errorFor(controlName: string): string | null {
    const control = this.form.get(controlName);
    if (!control || !control.touched || !control.errors) return null;

    const e = control.errors;
    if (e['server']) return e['server'] as string;           // server wins when present
    if (e['required'] || e['blank']) return 'Title is required.';
    if (e['maxlength']) {
      const max = e['maxlength'].requiredLength;
      const actual = e['maxlength'].actualLength;
      return `Maximum ${max} characters (currently ${actual}).`;
    }
    return null;
  }

  /** Clears a server error as soon as the person edits that field. */
  clearServerError(controlName: string): void {
    const control = this.form.get(controlName);
    if (control?.errors?.['server']) {
      const { server, ...rest } = control.errors;
      control.setErrors(Object.keys(rest).length ? rest : null);
    }
  }
}
