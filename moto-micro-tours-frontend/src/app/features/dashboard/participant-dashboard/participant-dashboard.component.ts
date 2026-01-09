import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { BookingsService } from '../../bookings/services/bookings';
import { BookingResponse, BookingStatus } from '../../bookings/models/booking.model';
import { DashboardHeaderComponent } from '../components/dashboard-header/dashboard-header.component';
import { BookingSectionListComponent } from '../components/booking-section-list/booking-section-list.component';
import { EmptyStateComponent } from '../components/empty-state/empty-state.component';

@Component({
  selector: 'app-participant-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    DashboardHeaderComponent,
    BookingSectionListComponent,
    EmptyStateComponent
  ],
  templateUrl: './participant-dashboard.component.html',
  styleUrl: './participant-dashboard.component.scss'
})
export class ParticipantDashboardComponent implements OnInit {
  private readonly bookingsService = inject(BookingsService);
  private readonly router = inject(Router);

  // Signals for state management
  protected readonly allBookings = signal<BookingResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  // Computed properties for filtered lists
  protected readonly pendingRequests = computed(() => {
    return this.allBookings().filter(
      booking => booking.status === BookingStatus.Pending
    );
  });

  protected readonly upcomingBookings = computed(() => {
    const now = new Date();
    return this.allBookings()
      .filter(booking => {
        const tourDate = new Date(booking.tourStartAt);
        const isUpcoming = tourDate >= now;
        const isConfirmed = booking.status === BookingStatus.Approved ||
                           booking.status === BookingStatus.Paid;
        return isUpcoming && isConfirmed;
      })
      .sort((a, b) => new Date(a.tourStartAt).getTime() - new Date(b.tourStartAt).getTime());
  });

  protected readonly pastBookings = computed(() => {
    const now = new Date();
    return this.allBookings()
      .filter(booking => {
        const tourDate = new Date(booking.tourStartAt);
        const isPast = tourDate < now;
        const isPaid = booking.status === BookingStatus.Paid;
        // Show all past paid bookings (whether completed or not)
        return isPast && isPaid;
      })
      .sort((a, b) => new Date(b.tourStartAt).getTime() - new Date(a.tourStartAt).getTime());
  });

  // Stage F7.1: Show declined bookings so participant can see decline reason
  protected readonly declinedBookings = computed(() => {
    return this.allBookings()
      .filter(booking => booking.status === BookingStatus.Declined)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  });

  // F8: Show cancelled bookings
  protected readonly cancelledBookings = computed(() => {
    return this.allBookings()
      .filter(booking =>
        booking.status === BookingStatus.Cancelled ||
        booking.status === BookingStatus.CancelledExpired ||
        booking.status === BookingStatus.Refunded
      )
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  });

  protected readonly nextBooking = computed(() => {
    const upcoming = this.upcomingBookings();
    return upcoming.length > 0 ? upcoming[0] : null;
  });

  BookingStatus = BookingStatus;

  ngOnInit(): void {
    this.loadBookings();
  }

  private loadBookings(): void {
    this.loading.set(true);
    this.error.set(null);

    this.bookingsService.getParticipantBookings().subscribe({
      next: (bookings) => {
        this.allBookings.set(bookings);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading bookings:', err);
        this.error.set(typeof err === 'string' ? err : 'Failed to load bookings');
        this.loading.set(false);
      }
    });
  }

  onBrowseTours(): void {
    this.router.navigate(['/tours']);
  }

  onViewBooking(bookingId: string): void {
    this.router.navigate(['/bookings', bookingId]);
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

  getStatusClass(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Pending:
        return 'status-pending';
      case BookingStatus.Approved:
        return 'status-approved';
      case BookingStatus.Paid:
        return 'status-paid';
      case BookingStatus.Declined:
        return 'status-declined';
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
      default:
        return status;
    }
  }

  retry(): void {
    this.loadBookings();
  }
}
