import { Component, input, output } from '@angular/core';

/**
 * [US-11] Deletion is permanent with no undo, so this confirmation is the only
 * safeguard the person has — which is why AC-11.4 requires it to say so plainly.
 */
@Component({
  selector: 'app-confirm-dialog',
  imports: [],
  templateUrl: './confirm-dialog.html',
})
export class ConfirmDialog {
  /** The task's title, so the person can see exactly what they are deleting (AC-11.3). */
  readonly taskTitle = input.required<string>();
  readonly busy = input<boolean>(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
