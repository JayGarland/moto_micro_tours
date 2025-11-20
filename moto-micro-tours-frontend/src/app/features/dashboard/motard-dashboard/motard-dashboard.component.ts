import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { BookingsService } from '../../bookings/services/bookings';
import { ToursService } from '../../tours/services/tours.service';
import { AuthService } from '../../../core/services/auth.service';
import { BookingSummary, BookingStatus } from '../../bookings/models/booking.model';
import { TourListDto } from '../../../core/models/tour.models';
import { DashboardHeaderComponent } from '../components/dashboard-header/dashboard-header.component';
import { BookingSectionListComponent } from '../components/booking-section-list/booking-section-list.component';
import { EmptyStateComponent } from '../components/empty-state/empty-state.component';

@Component({
  selector: 'app-motard-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    DashboardHeaderComponent,
    BookingSectionListComponent,
    EmptyStateComponent
  ],
  templateUrl: './motard-dashboard.component.html',
  styleUrl: './motard-dashboard.component.scss'
})
export class MotardDashboardComponent implements OnInit {
  private readonly bookingsService = inject(BookingsService);
  private readonly toursService = inject(ToursService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  // Signals for state management
  protected readonly allBookings = signal<BookingSummary[]>([]);
  protected readonly allTours = signal<TourListDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  private readonly currentUserId = this.authService.getCurrentUser()?.userId;

  // Computed properties for filtered lists
  protected readonly pendingApprovals = computed(() => {
    return this.allBookings().filter(
      booking => booking.status === BookingStatus.Pending
    );
  });

  protected readonly upcomingRides = computed(() => {
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

  protected readonly myTours = computed(() => {
    if (!this.currentUserId) return [];
    
    return this.allTours()
      .filter(tour => {
        // Check if current user is one of the motards in this tour
        return tour.motards.some(motard => motard.motardUserId === this.currentUserId);
      })
      .sort((a, b) => new Date(a.startAt).getTime() - new Date(b.startAt).getTime());
  });

  BookingStatus = BookingStatus;

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.loading.set(true);
    this.error.set(null);

    // Load both bookings and tours in parallel
    forkJoin({
      bookings: this.bookingsService.getMotardBookingRequests(),
      tours: this.toursService.getTours()
    }).subscribe({
      next: ({ bookings, tours }) => {
        this.allBookings.set(bookings);
        this.allTours.set(tours);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading dashboard data:', err);
        this.error.set(typeof err === 'string' ? err : 'Failed to load dashboard data');
        this.loading.set(false);
      }
    });
  }

  onCreateTour(): void {
    this.router.navigate(['/tours/create']);
  }

  onViewBooking(bookingId: string): void {
    this.router.navigate(['/bookings', bookingId]);
  }

  onViewTour(tourId: string): void {
    this.router.navigate(['/tours', tourId]);
  }

  onApproveBooking(bookingId: string): void {
    // TODO: Implement in Stage F7 - For now, show console message
    console.log('Approve booking:', bookingId);
    alert('Approve booking feature will be implemented in Stage F7');
    // Future implementation:
    // this.bookingsService.approveBooking(bookingId).subscribe({
    //   next: () => {
    //     this.loadDashboardData();
    //   },
    //   error: (err) => console.error('Error approving booking:', err)
    // });
  }

  onDeclineBooking(bookingId: string): void {
    // TODO: Implement in Stage F7 - For now, show console message
    console.log('Decline booking:', bookingId);
    alert('Decline booking feature will be implemented in Stage F7');
    // Future implementation:
    // const reason = prompt('Reason for declining (optional):');
    // this.bookingsService.declineBooking(bookingId, reason).subscribe({
    //   next: () => {
    //     this.loadDashboardData();
    //   },
    //   error: (err) => console.error('Error declining booking:', err)
    // });
  }

  formatDate(dateString: string): string {
    if (!dateString) return 'Date unavailable';
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) return 'Invalid date';
      return date.toLocaleDateString('fr-FR', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        timeZone: 'Europe/Paris'
      });
    } catch (error) {
      return 'Date unavailable';
    }
  }

  formatShortDate(dateString: string): string {
    if (!dateString) return 'Date unavailable';
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) return 'Invalid date';
      return date.toLocaleDateString('fr-FR', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        timeZone: 'Europe/Paris'
      });
    } catch (error) {
      return 'Date unavailable';
    }
  }

  formatPrice(price: number | null | undefined): string {
    if (price == null || isNaN(price)) {
      return 'Price to be defined';
    }
    return new Intl.NumberFormat('fr-FR', {
      style: 'currency',
      currency: 'EUR'
    }).format(price);
  }

  getTourStatus(tour: TourListDto): string {
    const tourDate = new Date(tour.startAt);
    const now = new Date();
    
    if (tourDate < now) {
      return 'Completed';
    }
    
    if (tour.totalSeatsBooked >= tour.totalCapacity) {
      return 'Full';
    }
    
    return tour.status;
  }

  getTourStatusClass(tour: TourListDto): string {
    const status = this.getTourStatus(tour);
    switch (status.toLowerCase()) {
      case 'completed':
        return 'tour-status-completed';
      case 'full':
        return 'tour-status-full';
      case 'published':
      case 'active':
        return 'tour-status-active';
      default:
        return 'tour-status-draft';
    }
  }

  retry(): void {
    this.loadDashboardData();
  }
}
