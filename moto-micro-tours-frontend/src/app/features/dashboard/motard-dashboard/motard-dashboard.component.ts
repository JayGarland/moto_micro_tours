import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { BookingsService } from '../../bookings/services/bookings';
import { ToursService } from '../../tours/services/tours.service';
import { AuthService } from '../../../core/services/auth.service';
import { BookingResponse, BookingStatus } from '../../bookings/models/booking.model';
import { TourListDto } from '../../../core/models/tour.models';
import { DashboardHeaderComponent } from '../components/dashboard-header/dashboard-header.component';
import { BookingSectionListComponent } from '../components/booking-section-list/booking-section-list.component';
import { EmptyStateComponent } from '../components/empty-state/empty-state.component';
import { ConfirmationDialogComponent, ConfirmationDialogData, ConfirmationDialogResult } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'app-motard-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatSnackBarModule,
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
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  // Signals for state management
  protected readonly allBookings = signal<BookingResponse[]>([]);
  protected readonly allTours = signal<TourListDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly processingBookingId = signal<string | null>(null);

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

  protected readonly pastRides = computed(() => {
    const now = new Date();
    return this.allBookings()
      .filter(booking => {
        const tourDate = new Date(booking.tourStartAt);
        const isPast = tourDate < now;
        const isPaid = booking.status === BookingStatus.Paid;
        return isPast && isPaid;
      })
      .sort((a, b) => new Date(b.tourStartAt).getTime() - new Date(a.tourStartAt).getTime());
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
    const dialogData: ConfirmationDialogData = {
      title: 'Approve Booking Request',
      message: 'Are you sure you want to approve this booking request? The participant will be notified and the booking will be confirmed.',
      confirmText: 'Approve',
      cancelText: 'Cancel',
      confirmColor: 'primary',
      icon: 'check_circle'
    };

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '450px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe((result: ConfirmationDialogResult | undefined) => {
      if (result?.confirmed) {
        this.approveBookingRequest(bookingId);
      }
    });
  }

  private approveBookingRequest(bookingId: string): void {
    this.processingBookingId.set(bookingId);

    this.bookingsService.approveBooking(bookingId).subscribe({
      next: () => {
        this.processingBookingId.set(null);
        this.snackBar.open('✅ Booking approved! Check "Upcoming rides" section below.', 'Close', {
          duration: 5000,
          horizontalPosition: 'end',
          verticalPosition: 'top',
          panelClass: ['success-snackbar']
        });
        this.loadDashboardData();
      },
      error: (err) => {
        this.processingBookingId.set(null);
        console.error('Error approving booking:', err);
        this.snackBar.open(
          typeof err === 'string' ? err : 'Failed to approve booking. Please try again.',
          'Close',
          {
            duration: 5000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['error-snackbar']
          }
        );
      }
    });
  }

  onDeclineBooking(bookingId: string): void {
    const dialogData: ConfirmationDialogData = {
      title: 'Decline Booking Request',
      message: 'Are you sure you want to decline this booking request? The participant will be notified.',
      confirmText: 'Decline',
      cancelText: 'Cancel',
      confirmColor: 'warn',
      icon: 'cancel',
      showTextarea: true, // Stage F7.1
      textareaLabel: 'Reason (optional)', // Stage F7.1
      textareaPlaceholder: 'Explain why you\'re declining this booking...' // Stage F7.1
    };

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '500px',
      data: dialogData
    });

    dialogRef.afterClosed().subscribe((result: ConfirmationDialogResult | undefined) => {
      if (result?.confirmed) {
        this.declineBookingRequest(bookingId, result.textValue);
      }
    });
  }

  private declineBookingRequest(bookingId: string, reason?: string): void {
    this.processingBookingId.set(bookingId);

    this.bookingsService.declineBooking(bookingId, reason).subscribe({
      next: () => {
        this.processingBookingId.set(null);
        this.snackBar.open('Booking declined successfully.', 'Close', {
          duration: 3000,
          horizontalPosition: 'end',
          verticalPosition: 'top',
          panelClass: ['success-snackbar']
        });
        this.loadDashboardData();
      },
      error: (err) => {
        this.processingBookingId.set(null);
        console.error('Error declining booking:', err);
        this.snackBar.open(
          typeof err === 'string' ? err : 'Failed to decline booking. Please try again.',
          'Close',
          {
            duration: 5000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            panelClass: ['error-snackbar']
          }
        );
      }
    });
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

  isProcessing(bookingId: string): boolean {
    return this.processingBookingId() === bookingId;
  }
}
