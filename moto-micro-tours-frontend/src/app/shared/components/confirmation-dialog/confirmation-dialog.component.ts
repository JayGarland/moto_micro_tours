import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';

export interface ConfirmationDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  confirmColor?: 'primary' | 'accent' | 'warn';
  icon?: string;
  showTextarea?: boolean; // Stage F7.1 - for decline reason
  textareaPlaceholder?: string; // Stage F7.1
  textareaLabel?: string; // Stage F7.1
}

export interface ConfirmationDialogResult {
  confirmed: boolean;
  textValue?: string; // Stage F7.1 - for decline reason
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule
  ],
  template: `
    <div class="confirmation-dialog">
      <h2 mat-dialog-title class="dialog-title">
        <mat-icon *ngIf="data.icon" class="dialog-icon">{{ data.icon }}</mat-icon>
        {{ data.title }}
      </h2>

      <mat-dialog-content class="dialog-content">
        <p>{{ data.message }}</p>

        <!-- Stage F7.1: Optional textarea for decline reason -->
        <mat-form-field *ngIf="data.showTextarea" appearance="outline" class="textarea-field">
          <mat-label>{{ data.textareaLabel || 'Reason' }}</mat-label>
          <textarea
            matInput
            [(ngModel)]="textInput"
            [placeholder]="data.textareaPlaceholder || 'Enter reason (optional)'"
            rows="3"
            maxlength="500"
          ></textarea>
          <mat-hint align="end">{{ textInput.length }} / 500</mat-hint>
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end" class="dialog-actions">
        <button
          mat-button
          (click)="onCancel()"
          class="cancel-button"
        >
          {{ data.cancelText || 'Cancel' }}
        </button>
        <button
          mat-raised-button
          [color]="data.confirmColor || 'primary'"
          (click)="onConfirm()"
          class="confirm-button"
        >
          {{ data.confirmText || 'Confirm' }}
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .confirmation-dialog {
      min-width: 320px;
      max-width: 500px;
    }

    .dialog-title {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-size: 1.25rem;
      font-weight: 600;
      margin: 0;
      padding: 1.5rem 1.5rem 1rem;
    }

    .dialog-icon {
      font-size: 1.75rem;
      width: 1.75rem;
      height: 1.75rem;
      color: var(--color-primary, #0066b2);
    }

    .dialog-content {
      padding: 0 1.5rem 1.5rem;
      font-size: 0.95rem;
      line-height: 1.6;
      color: var(--color-text-secondary, #6b7280);

      p {
        margin: 0 0 1rem 0;
      }
    }

    .textarea-field {
      width: 100%;
      margin-top: 0.5rem;
    }

    .dialog-actions {
      padding: 1rem 1.5rem;
      border-top: 1px solid var(--color-border, #e5e7eb);
      gap: 0.75rem;
    }

    .cancel-button {
      color: var(--color-text-secondary, #6b7280);
    }

    .confirm-button {
      min-width: 100px;
    }
  `]
})
export class ConfirmationDialogComponent {
  textInput = ''; // Stage F7.1 - for decline reason

  constructor(
    public dialogRef: MatDialogRef<ConfirmationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmationDialogData
  ) {}

  onConfirm(): void {
    // Stage F7.1: Return both confirmation and text value
    const result: ConfirmationDialogResult = {
      confirmed: true,
      textValue: this.data.showTextarea ? this.textInput : undefined
    };
    this.dialogRef.close(result);
  }

  onCancel(): void {
    const result: ConfirmationDialogResult = {
      confirmed: false
    };
    this.dialogRef.close(result);
  }
}
