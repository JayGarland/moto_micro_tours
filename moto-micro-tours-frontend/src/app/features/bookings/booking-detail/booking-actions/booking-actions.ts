import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BookingStatus } from '../../models/booking.model';

@Component({
  selector: 'app-booking-actions',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './booking-actions.html',
  styleUrl: './booking-actions.scss',
})
export class BookingActions {
  @Input() status!: BookingStatus;
  @Input() bookingId!: string;
  @Input() canPay: boolean = false;
  @Input() canCancel: boolean = false;
  @Input() expiresAt?: string;

  @Output() pay = new EventEmitter<void>();
  @Output() refresh = new EventEmitter<void>();
  @Output() openChat = new EventEmitter<void>();

  // Expose enum for template
  BookingStatus = BookingStatus;

  onPay(): void {
    this.pay.emit();
  }

  onRefresh(): void {
    this.refresh.emit();
  }

  onOpenChat(): void {
    this.openChat.emit();
  }

  getStatusMessage(): string {
    switch (this.status) {
      case BookingStatus.Pending:
        return 'Your booking request has been sent to the motard. You\'ll be notified once they respond.';
      case BookingStatus.Approved:
        return 'Your booking has been approved! Please complete the payment to confirm your spot.';
      case BookingStatus.Paid:
        return 'Payment completed! You\'re all set for the tour. You can now chat with the motard.';
      case BookingStatus.Declined:
        return 'Unfortunately, the motard declined this booking request.';
      case BookingStatus.Cancelled:
        return 'This booking has been cancelled.';
      case BookingStatus.CancelledExpired:
        return 'This booking was cancelled because the payment window expired.';
      case BookingStatus.Refunded:
        return 'This booking has been refunded.';
      default:
        return '';
    }
  }

  formatExpiryDate(dateString?: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
