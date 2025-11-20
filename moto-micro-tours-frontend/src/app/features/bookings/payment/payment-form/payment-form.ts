import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';

import { BookingsService } from '../../services/bookings';
import { BookingDetail, BookingStatus } from '../../models/booking.model';

@Component({
  selector: 'app-payment-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatDialogModule,
  ],
  templateUrl: './payment-form.html',
  styleUrl: './payment-form.scss',
})
export class PaymentForm implements OnInit {
  // State signals
  isLoadingBooking = signal(true);
  isProcessingPayment = signal(false);
  hasPaid = signal(false);
  errorMessage = signal<string | null>(null);

  // Data
  booking: BookingDetail | null = null;
  bookingId: string = '';

  // Payment form
  paymentForm: FormGroup;

  // Computed properties
  get totalPrice(): number {
    return this.booking?.pricePerSeat ?? 0;
  }

  get motardShare(): number {
    // Use explicit amounts if backend provides them, otherwise 50/50
    if (this.booking?.payout) {
      return this.booking.payout.motardShare;
    }
    return this.totalPrice / 2;
  }

  get platformShare(): number {
    if (this.booking?.payout) {
      return this.booking.payout.platformShare;
    }
    return this.totalPrice / 2;
  }

  get isEligibleForPayment(): boolean {
    if (!this.booking) return false;
    return this.booking.status === BookingStatus.Approved;
  }

  get isPaid(): boolean {
    return this.booking?.status === BookingStatus.Paid;
  }

  get isCancelledOrDeclined(): boolean {
    if (!this.booking) return false;
    return this.booking.status === BookingStatus.Cancelled ||
           this.booking.status === BookingStatus.Declined ||
           this.booking.status === BookingStatus.CancelledExpired;
  }

  get canProcessPayment(): boolean {
    return this.isEligibleForPayment &&
           !this.isPaid &&
           !this.isCancelledOrDeclined &&
           !this.isProcessingPayment();
  }

  constructor(
    private bookingsService: BookingsService,
    private router: Router,
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private dialog: MatDialog
  ) {
    // Initialize payment form
    this.paymentForm = this.fb.group({
      cardSegment1: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]],
      cardSegment2: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]],
      cardSegment3: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]],
      cardSegment4: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]],
      cardName: ['', [Validators.required, Validators.minLength(3)]],
      cardExpiry: ['', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]],
    });
  }

  ngOnInit(): void {
    // Get booking ID from route params
    this.bookingId = this.route.snapshot.paramMap.get('bookingId') ?? '';

    if (!this.bookingId) {
      this.errorMessage.set('No booking ID provided');
      this.isLoadingBooking.set(false);
      return;
    }

    this.loadBooking();
  }

  loadBooking(): void {
    this.isLoadingBooking.set(true);
    this.errorMessage.set(null);

    this.bookingsService.getBookingById(this.bookingId).subscribe({
      next: (booking) => {
        this.booking = booking;
        this.isLoadingBooking.set(false);

        // Disable form if not eligible for payment
        if (!this.canProcessPayment) {
          this.paymentForm.disable();
        }
      },
      error: (error) => {
        console.error('Failed to load booking:', error);
        this.errorMessage.set(error || 'Failed to load booking details');
        this.isLoadingBooking.set(false);
      }
    });
  }

  onConfirmPayment(): void {
    // Validate form
    if (!this.paymentForm.valid) {
      Object.keys(this.paymentForm.controls).forEach(key => {
        this.paymentForm.get(key)?.markAsTouched();
      });
      return;
    }

    // Show confirmation dialog
    const dialogRef = this.dialog.open(PaymentConfirmationDialog, {
      width: '400px',
      data: { amount: this.totalPrice }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.processPayment();
      }
    });
  }

  private processPayment(): void {
    this.errorMessage.set(null);
    this.isProcessingPayment.set(true);
    this.paymentForm.disable();

    // Simulate 2-second processing delay
    setTimeout(() => {
      this.bookingsService.markBookingAsPaid(this.bookingId).subscribe({
        next: () => {
          this.hasPaid.set(true);

          // Update local booking status
          if (this.booking) {
            this.booking.status = BookingStatus.Paid;
          }

          // Auto-redirect after 3 seconds
          setTimeout(() => {
            this.navigateToBooking();
          }, 3000);
        },
        error: (error) => {
          console.error('Payment failed:', error);
          this.errorMessage.set(error || 'Payment processing failed. Please try again.');
          this.isProcessingPayment.set(false);
          this.paymentForm.enable();
        }
      });
    }, 2000);
  }

  navigateToBooking(): void {
    this.router.navigate(['/bookings', this.bookingId]);
  }

  goBack(): void {
    this.router.navigate(['/bookings', this.bookingId]);
  }

  // Format date in French timezone
  formatDate(dateString?: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Europe/Paris'
    });
  }

  // Helper for form validation display
  getFieldError(fieldName: string): string {
    const control = this.paymentForm.get(fieldName);
    if (!control?.touched || !control?.errors) return '';

    if (control.errors['required']) return 'This field is required';
    if (control.errors['pattern']) {
      if (fieldName.startsWith('cardSegment')) return 'Must be 4 digits';
      if (fieldName === 'cardExpiry') return 'Format: MM/YY';
    }
    if (control.errors['minLength']) return 'Name too short';

    return 'Invalid input';
  }
}

// Confirmation Dialog Component
@Component({
  selector: 'payment-confirmation-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, CommonModule],
  template: `
    <h2 mat-dialog-title>Confirm Payment</h2>
    <mat-dialog-content>
      <p>Are you sure you want to proceed with the payment of <strong>€{{data.amount.toFixed(2)}}</strong>?</p>
      <p class="warning-text">This action cannot be undone.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">Cancel</button>
      <button mat-raised-button color="primary" [mat-dialog-close]="true">Confirm Payment</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .warning-text {
      color: #f44336;
      font-size: 0.9em;
      margin-top: 8px;
    }
    mat-dialog-actions {
      padding: 16px 0 0 0;
    }
  `]
})
export class PaymentConfirmationDialog {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { amount: number }) {}
}

// Import for dialog data injection
import { Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
