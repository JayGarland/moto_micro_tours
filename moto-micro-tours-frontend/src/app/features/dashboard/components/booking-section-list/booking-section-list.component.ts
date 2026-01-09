import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BookingResponse, BookingStatus } from '../../../bookings/models/booking.model';
import { EmptyStateComponent } from '../empty-state/empty-state.component';

@Component({
  selector: 'app-booking-section-list',
  standalone: true,
  imports: [CommonModule, RouterLink, EmptyStateComponent],
  templateUrl: './booking-section-list.component.html',
  styleUrl: './booking-section-list.component.scss'
})
export class BookingSectionListComponent {
  @Input() title: string = '';
  @Input() bookings: BookingResponse[] = [];
  @Input() emptyMessage: string = 'No bookings found';
  @Input() emptyIcon: string = '📭';

  BookingStatus = BookingStatus;

  getStatusClass(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Pending:
        return 'status-pending';
      case BookingStatus.Approved:
        return 'status-approved';
      case BookingStatus.Paid:
        return 'status-paid';
      case BookingStatus.Declined:
      case BookingStatus.Cancelled:
      case BookingStatus.CancelledExpired:
        return 'status-cancelled';
      case BookingStatus.Refunded:
        return 'status-refunded';
      default:
        return '';
    }
  }

  getStatusLabel(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Pending:
        return 'Pending approval';
      case BookingStatus.Approved:
        return 'Approved';
      case BookingStatus.Paid:
        return 'Paid';
      case BookingStatus.Declined:
        return 'Declined';
      case BookingStatus.Cancelled:
        return 'Cancelled';
      case BookingStatus.CancelledExpired:
        return 'Expired';
      case BookingStatus.Refunded:
        return 'Refunded';
      default:
        return status;
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Europe/Paris'
    });
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('fr-FR', {
      style: 'currency',
      currency: 'EUR'
    }).format(price);
  }
}
