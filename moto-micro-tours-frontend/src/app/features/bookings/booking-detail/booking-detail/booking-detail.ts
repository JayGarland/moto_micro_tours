import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { BookingsService } from '../../services/bookings';
import { BookingDetail as BookingDetailModel, BookingStatus } from '../../models/booking.model';
import { BookingStatusTimeline } from '../booking-status-timeline/booking-status-timeline';
import { BookingActions } from '../booking-actions/booking-actions';

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    BookingStatusTimeline,
    BookingActions
  ],
  templateUrl: './booking-detail.html',
  styleUrl: './booking-detail.scss',
})
export class BookingDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly bookingsService = inject(BookingsService);

  // UI State
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  booking = signal<BookingDetailModel | null>(null);
  bookingId = signal<string>('');

  // Expose enum for template
  BookingStatus = BookingStatus;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('bookingId');
    if (!id) {
      this.errorMessage.set('Invalid booking ID');
      this.isLoading.set(false);
      return;
    }
    this.bookingId.set(id);
    this.loadBooking();
  }

  loadBooking(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.bookingsService.getBookingById(this.bookingId()).subscribe({
      next: (booking) => {
        this.booking.set(booking);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error || 'Failed to load booking details');
        this.isLoading.set(false);
      }
    });
  }

  onRefresh(): void {
    this.loadBooking();
  }

  onPay(): void {
    this.router.navigate(['/bookings', this.bookingId(), 'pay']);
  }

  onOpenChat(): void {
    const bookingData = this.booking();
    if (!bookingData) return;

    // If there's a preBookingConversationId, navigate directly to it
    // Otherwise, use motardId + tourId for conversation creation flow
    this.router.navigate(['/messages'], {
      queryParams: {
        conversationId: (bookingData as any).preBookingConversationId,
        motardId: bookingData.motardUserId,
        tourId: bookingData.tourId
      }
    });
  }

  getStatusColor(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Pending:
        return 'accent';
      case BookingStatus.Approved:
        return 'primary';
      case BookingStatus.Paid:
        return 'primary';
      case BookingStatus.Declined:
        return 'warn';
      case BookingStatus.Cancelled:
      case BookingStatus.CancelledExpired:
        return 'warn';
      case BookingStatus.Refunded:
        return '';
      default:
        return '';
    }
  }

  getStatusLabel(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Pending:
        return 'Pending Approval';
      case BookingStatus.Approved:
        return 'Approved - Payment Required';
      case BookingStatus.Paid:
        return 'Payment Completed';
      case BookingStatus.Declined:
        return 'Declined by Motard';
      case BookingStatus.Cancelled:
        return 'Cancelled';
      case BookingStatus.CancelledExpired:
        return 'Cancelled (Expired)';
      case BookingStatus.Refunded:
        return 'Refunded';
      default:
        return status;
    }
  }

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

  canPay(): boolean {
    const bookingData = this.booking();
    return bookingData?.status === BookingStatus.Approved;
  }

  canCancel(): boolean {
    const bookingData = this.booking();
    return bookingData?.status === BookingStatus.Pending ||
           bookingData?.status === BookingStatus.Approved;
  }
}
