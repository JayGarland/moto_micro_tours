import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BookingsService } from '../../services/bookings';
import { BookingDetail as BookingDetailModel, BookingStatus, ApproveStartTimeRequest } from '../../models/booking.model';
import { BookingStatusTimeline } from '../booking-status-timeline/booking-status-timeline';
import { BookingActions } from '../booking-actions/booking-actions';
import { BookingChatComponent } from '../../../chats/booking-chat/booking-chat.component';
import { BookingReviewForm } from '../booking-review-form/booking-review-form';
import { ReviewCardComponent } from '../../../profile/motard-profile/review-card.component';
import { AuthService } from '../../../../core/services/auth.service';
import { ConfirmationDialogComponent, ConfirmationDialogData } from '../../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { ReviewsService } from '../../services/reviews.service';
import { ReviewTargetType } from '../../models/review.models';
import { extractErrorMessage } from '../../../../shared/utils';

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
    BookingActions,
    BookingChatComponent,
    BookingReviewForm,
    ReviewCardComponent
  ],
  templateUrl: './booking-detail.html',
  styleUrl: './booking-detail.scss',
})
export class BookingDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly bookingsService = inject(BookingsService);
  readonly authService = inject(AuthService); // Public for template access
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly reviewsService = inject(ReviewsService);

  // UI State
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  booking = signal<BookingDetailModel | null>(null);
  bookingId = signal<string>('');

  // F8: Review state
  showReviewForm = signal(false);
  isSubmittingReview = signal(false);
  bookingReviews = signal<any[]>([]); // All reviews from this booking (tour, motard, participant)
  isLoadingReviews = signal(false);

  // Expose enum for template
  BookingStatus = BookingStatus;

  // F7.2: Computed properties for chat visibility and partner info
  canShowChat = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Check if user is participant or motard
    const isParticipant = bookingData.participantId === currentUser.userId;
    const isMotard = bookingData.motardUserId === currentUser.userId;
    if (!isParticipant && !isMotard) return false;

    // Check if booking status allows chat (Approved, Paid, Cancelled, CancelledExpired, Refunded)
    return bookingData.status === BookingStatus.Approved ||
           bookingData.status === BookingStatus.Paid ||
           bookingData.status === BookingStatus.Cancelled ||
           bookingData.status === BookingStatus.CancelledExpired ||
           bookingData.status === BookingStatus.Refunded;
  });

  isMotardViewer = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;
    return bookingData.motardUserId === currentUser.userId;
  });

  chatPartnerName = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return '';

    // If current user is participant, show motard name. Otherwise, show participant name.
    const isParticipant = bookingData.participantId === currentUser.userId;
    return isParticipant ? bookingData.motardName : bookingData.participantName;
  });

  counterpartLabel = computed(() => (this.isMotardViewer() ? 'Participant' : 'Motard'));

  counterpartName = computed(() => {
    const bookingData = this.booking();
    if (!bookingData) return '';
    return this.isMotardViewer() ? bookingData.participantName : bookingData.motardName;
  });

  // F8: Check if participant can cancel (respecting 24h rule)
  canParticipantCancel = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Must be the participant
    const isParticipant = bookingData.participantId === currentUser.userId;
    if (!isParticipant) return false;

    // Must be Pending or Approved
    const validStatus = bookingData.status === BookingStatus.Pending ||
                       bookingData.status === BookingStatus.Approved;
    if (!validStatus) return false;

    return true;
  });

  // F8: Check if cancellation is within 24h (for paid bookings)
  isWithin24Hours = computed(() => {
    const bookingData = this.booking();
    if (!bookingData || !bookingData.tourStartAt) return false;

    const tourStartTime = new Date(bookingData.tourStartAt).getTime();
    const now = new Date().getTime();
    const hoursUntilTour = (tourStartTime - now) / (1000 * 60 * 60);

    return hoursUntilTour < 24;
  });

  // F8: Show cancel button for paid bookings only if >24h
  canCancelPaidBooking = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Must be the participant
    const isParticipant = bookingData.participantId === currentUser.userId;
    if (!isParticipant) return false;

    // Must be Paid
    if (bookingData.status !== BookingStatus.Paid) return false;

    // Must be >24h before tour
    return !this.isWithin24Hours();
  });

  // F8: Check if motard can refund
  canMotardRefund = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Must be the motard
    const isMotard = bookingData.motardUserId === currentUser.userId;
    if (!isMotard) return false;

    // Must be Paid status
    return bookingData.status === BookingStatus.Paid;
  });

  // F8: Check if user can leave review (B3: requires booking completion)
  canLeaveReview = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Must be the participant or the motard
    const isParticipant = bookingData.participantId === currentUser.userId;
    const isMotard = bookingData.motardUserId === currentUser.userId;
    if (!isParticipant && !isMotard) return false;

    // Must be Paid status
    if (bookingData.status !== BookingStatus.Paid) return false;

    // Must be completed (both parties confirmed completion)
    if (!bookingData.completedAtUtc) return false;

    return true;
  });

  // F8: Check if user can mark booking as complete
  canMarkComplete = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Must be participant or motard
    const isParticipant = bookingData.participantId === currentUser.userId;
    const isMotard = bookingData.motardUserId === currentUser.userId;
    if (!isParticipant && !isMotard) return false;

    // Must be Paid status
    if (bookingData.status !== BookingStatus.Paid) return false;

    // Option E: Use EffectiveStartAtUtc (ApprovedStartAtUtc ?? TourStartAt)
    const effectiveStartTime = new Date(bookingData.approvedStartAtUtc || bookingData.tourStartAt);
    const now = new Date();
    if (effectiveStartTime > now) return false;

    // Check if user already marked as complete
    if (isParticipant && bookingData.completedByParticipantAtUtc) return false;
    if (isMotard && bookingData.completedByMotardAtUtc) return false;

    return true;
  });

  // F8: Check if booking is fully completed
  isFullyCompleted = computed(() => {
    const bookingData = this.booking();
    return bookingData?.completedAtUtc ? true : false;
  });

  // F8: Check if waiting for other party to complete
  isWaitingForOtherSide = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // If fully completed, not waiting
    if (bookingData.completedAtUtc) return false;

    // Check if current user completed but other side hasn't
    const isParticipant = bookingData.participantId === currentUser.userId;
    const isMotard = bookingData.motardUserId === currentUser.userId;

    if (isParticipant && bookingData.completedByParticipantAtUtc && !bookingData.completedByMotardAtUtc) {
      return true;
    }
    if (isMotard && bookingData.completedByMotardAtUtc && !bookingData.completedByParticipantAtUtc) {
      return true;
    }

    return false;
  });

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
        // Load reviews for this booking
        this.loadBookingReviews();
      },
      error: (error) => {
        this.errorMessage.set(error || 'Failed to load booking details');
        this.isLoading.set(false);
      }
    });
  }

  loadBookingReviews(): void {
    this.isLoadingReviews.set(true);
    this.reviewsService.getReviewsByBooking(this.bookingId()).subscribe({
      next: (reviews) => {
        this.bookingReviews.set(reviews);
        this.isLoadingReviews.set(false);
      },
      error: (error) => {
        console.error('Failed to load reviews:', error);
        this.isLoadingReviews.set(false);
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

    // For bookings with chat enabled, scroll to the booking chat section
    setTimeout(() => {
      const chatElement = document.getElementById('booking-chat-section');
      if (chatElement) {
        chatElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    }, 100);
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
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Only participant can pay
    const isParticipant = bookingData.participantId === currentUser.userId;
    return isParticipant && bookingData.status === BookingStatus.Approved;
  }

  canCancel(): boolean {
    const bookingData = this.booking();
    return bookingData?.status === BookingStatus.Pending ||
           bookingData?.status === BookingStatus.Approved;
  }

  // F8: Cancel booking
  onCancelBooking(): void {
    const bookingData = this.booking();
    if (!bookingData) return;

    const isPaid = bookingData.status === BookingStatus.Paid;
    const dialogData: ConfirmationDialogData = {
      title: 'Cancel Booking',
      message: isPaid
        ? `You are about to cancel this paid booking. Since there is more than 24 hours before the tour starts, you will receive a full refund. Are you sure you want to proceed?`
        : `You are about to cancel this booking. Are you sure you want to proceed?`,
      confirmText: 'Yes, Cancel Booking',
      cancelText: 'Keep Booking',
      confirmColor: 'warn',
      icon: 'cancel'
    };

    this.dialog.open(ConfirmationDialogComponent, { data: dialogData }).afterClosed().subscribe(result => {
      if (result?.confirmed) {
        this.isLoading.set(true);
        this.bookingsService.cancelBooking(bookingData.bookingId).subscribe({
          next: () => {
            this.snackBar.open('Booking cancelled successfully', 'Close', { duration: 5000 });
            this.loadBooking();
          },
          error: (error) => {
            this.snackBar.open(extractErrorMessage(error, 'Failed to cancel booking'), 'Close', { duration: 5000 });
            this.isLoading.set(false);
          }
        });
      }
    });
  }

  // F8: Refund booking (motard only)
  onRefundBooking(): void {
    const bookingData = this.booking();
    if (!bookingData) return;

    const dialogData: ConfirmationDialogData = {
      title: 'Refund Booking',
      message: `You are about to refund this booking. The participant will receive a full refund and the payout will be cancelled. This action cannot be undone. Are you sure?`,
      confirmText: 'Yes, Issue Refund',
      cancelText: 'Cancel',
      confirmColor: 'warn',
      icon: 'money_off'
    };

    this.dialog.open(ConfirmationDialogComponent, { data: dialogData }).afterClosed().subscribe(result => {
      if (result?.confirmed) {
        this.isLoading.set(true);
        this.bookingsService.refundBooking(bookingData.bookingId).subscribe({
          next: () => {
            this.snackBar.open('Booking refunded successfully', 'Close', { duration: 5000 });
            this.loadBooking();
          },
          error: (error) => {
            this.snackBar.open(extractErrorMessage(error, 'Failed to refund booking'), 'Close', { duration: 5000 });
            this.isLoading.set(false);
          }
        });
      }
    });
  }

  // F8: Mark booking as completed
  onMarkComplete(): void {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return;

    const isParticipant = bookingData.participantId === currentUser.userId;
    const isMotard = bookingData.motardUserId === currentUser.userId;
    const waitingForOther = this.isWaitingForOtherSide();

    // Determine appropriate messaging
    const userRole = isParticipant ? 'participant' : 'motard';
    const otherRole = isParticipant ? 'motard' : 'participant';

    let message: string;
    if (bookingData.completedByParticipantAtUtc && !isParticipant) {
      message = `The participant has marked this booking as complete. Click confirm to finalize completion on your end.`;
    } else if (bookingData.completedByMotardAtUtc && !isMotard) {
      message = `The motard has marked this booking as complete. Click confirm to finalize completion on your end.`;
    } else {
      message = `Mark this booking as completed on your end? The ${otherRole} will also need to confirm completion.`;
    }

    const dialogData: ConfirmationDialogData = {
      title: 'Confirm Booking Completion',
      message,
      confirmText: 'Mark as Completed',
      cancelText: 'Not Yet',
      confirmColor: 'primary',
      icon: 'check_circle'
    };

    this.dialog.open(ConfirmationDialogComponent, { data: dialogData }).afterClosed().subscribe(result => {
      if (result?.confirmed) {
        this.isLoading.set(true);
        this.bookingsService.completeBooking(bookingData.bookingId).subscribe({
          next: () => {
            const successMsg = waitingForOther
              ? 'Booking fully completed! Both parties have confirmed.'
              : 'Marked as completed. Waiting for the other party to confirm.';
            this.snackBar.open(successMsg, 'Close', { duration: 5000 });
            this.loadBooking();
          },
          error: (error) => {
            this.snackBar.open(extractErrorMessage(error, 'Failed to mark booking as complete'), 'Close', { duration: 5000 });
            this.isLoading.set(false);
          }
        });
      }
    });
  }

  // F8: Show review form
  onShowReviewForm(): void {
    this.showReviewForm.set(true);
  }

  // F8: Hide review form
  onCancelReview(): void {
    this.showReviewForm.set(false);
  }

  // F8: Submit reviews (tour and/or motard/participant)
  onSubmitReview(reviewsData: {
    tourReview: { rating: number; comment: string } | null;
    motardReview: { rating: number; comment: string } | null;
    participantReview: { rating: number; comment: string } | null;
  }): void {
    const bookingData = this.booking();
    if (!bookingData) return;

    this.isSubmittingReview.set(true);
    const reviewPromises: Promise<any>[] = [];

    // Submit tour review if provided
    if (reviewsData.tourReview) {
      const tourReviewPromise = this.reviewsService.createReview({
        bookingId: bookingData.bookingId,
        targetType: ReviewTargetType.Tour,
        targetId: bookingData.tourId,
        rating: reviewsData.tourReview.rating,
        comment: reviewsData.tourReview.comment
      }).toPromise();
      reviewPromises.push(tourReviewPromise);
    }

    // Submit motard review if provided (participant reviewing motard)
    if (reviewsData.motardReview) {
      const motardReviewPromise = this.reviewsService.createReview({
        bookingId: bookingData.bookingId,
        targetType: ReviewTargetType.User,
        targetId: bookingData.motardUserId,
        rating: reviewsData.motardReview.rating,
        comment: reviewsData.motardReview.comment
      }).toPromise();
      reviewPromises.push(motardReviewPromise);
    }

    // Submit participant review if provided (motard reviewing participant)
    if (reviewsData.participantReview) {
      const participantReviewPromise = this.reviewsService.createReview({
        bookingId: bookingData.bookingId,
        targetType: ReviewTargetType.User,
        targetId: bookingData.participantId,
        rating: reviewsData.participantReview.rating,
        comment: reviewsData.participantReview.comment
      }).toPromise();
      reviewPromises.push(participantReviewPromise);
    }

    // Wait for all reviews to complete
    Promise.all(reviewPromises).then(
      () => {
        const reviewCount = reviewPromises.length;
        const message = reviewCount === 2
          ? 'Both reviews submitted successfully!'
          : 'Review submitted successfully!';
        this.snackBar.open(message, 'Close', { duration: 5000 });
        this.showReviewForm.set(false);
        this.isSubmittingReview.set(false);
        // Reload reviews to show the new ones
        this.loadBookingReviews();
      },
      (error) => {
        const errorMsg = extractErrorMessage(error, 'Failed to submit reviews');
        this.snackBar.open(errorMsg, 'Close', { duration: 5000 });
        this.isSubmittingReview.set(false);
      }
    );
  }

  // Option E: Approve start time (motard only)
  onApproveStartTime(): void {
    const bookingData = this.booking();
    if (!bookingData) return;

    // Prompt for approved start time
    const now = new Date();
    const defaultTime = now.toISOString().slice(0, 16); // Format: YYYY-MM-DDTHH:mm

    const approvedTime = prompt(
      'Enter approved start time (YYYY-MM-DDTHH:mm format):',
      defaultTime
    );

    if (!approvedTime) return;

    const reason = prompt('Enter reason for approving early start (optional):', '');

    const request: ApproveStartTimeRequest = {
      approvedStartAtUtc: new Date(approvedTime).toISOString(),
      reason: reason || undefined
    };

    this.isLoading.set(true);
    this.bookingsService.approveStartTime(bookingData.bookingId, request).subscribe({
      next: () => {
        this.snackBar.open('Effective start time approved successfully', 'Close', { duration: 5000 });
        this.loadBooking();
      },
      error: (error) => {
        this.snackBar.open(extractErrorMessage(error, 'Failed to approve start time'), 'Close', { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  // Option E: Check if motard can approve start time
  canApproveStartTime = computed(() => {
    const bookingData = this.booking();
    const currentUser = this.authService.getCurrentUser();
    if (!bookingData || !currentUser) return false;

    // Must be the motard or tour organizer
    const isMotard = bookingData.motardUserId === currentUser.userId;
    const isOrganizer = bookingData.organizerUserId === currentUser.userId;
    if (!isMotard && !isOrganizer) return false;

    // Must be Paid status
    if (bookingData.status !== BookingStatus.Paid) return false;

    // Don't show if already approved
    if (bookingData.approvedStartAtUtc) return false;

    return true;
  });

  // Option E: Get effective start time
  getEffectiveStartTime(): Date {
    const bookingData = this.booking();
    if (!bookingData) return new Date();

    // EffectiveStartAtUtc = ApprovedStartAtUtc ?? TourStartAt
    return new Date(bookingData.approvedStartAtUtc || bookingData.tourStartAt);
  }
}

