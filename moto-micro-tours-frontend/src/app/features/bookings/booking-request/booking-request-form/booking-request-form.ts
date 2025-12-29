import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { BookingsService } from '../../services/bookings';
import { CreateBookingRequest } from '../../models/booking.model';

interface TourSummary {
  tourId: string;
  tourMotardId: string;
  title: string;
  startAt: string;
  startLocationText: string;
  endLocationText: string;
  pricePerSeat: number;
  motardName: string;
}

@Component({
  selector: 'app-booking-request-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule
  ],
  templateUrl: './booking-request-form.html',
  styleUrl: './booking-request-form.scss',
})
export class BookingRequestForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly bookingsService = inject(BookingsService);
  private readonly dialog = inject(MatDialog);

  // UI State
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);
  tourSummary = signal<TourSummary | null>(null);

  // Form
  bookingForm!: FormGroup;

  ngOnInit(): void {
    this.initializeForm();
    this.loadTourData();
  }

  private initializeForm(): void {
    this.bookingForm = this.fb.group({
      emergencyContact: ['', [Validators.required, Validators.minLength(3)]],
      emergencyPhone: ['', [Validators.required, Validators.pattern(/^[\d\s\-\+\(\)]+$/)]],
      acceptSafetyRules: [false, Validators.requiredTrue],
      costSharingOptIn: [false],
      messageToMotard: ['']
    });
  }

  private loadTourData(): void {
    // Priority 1: Navigation state from window.history.state
    // getCurrentNavigation() is null after component initializes, so use window.history.state
    const navigationState = window.history.state;

    if (navigationState?.['tourId'] && navigationState?.['tourMotardId']) {
      const tourSummary = navigationState['tourSummary'];
      this.tourSummary.set({
        tourId: navigationState['tourId'],
        tourMotardId: navigationState['tourMotardId'],
        title: tourSummary?.title || 'Tour',
        startAt: tourSummary?.date || '',
        startLocationText: tourSummary?.startLocation || '',
        endLocationText: tourSummary?.endLocation || '',
        pricePerSeat: tourSummary?.price || 0,
        motardName: tourSummary?.motardName || 'Motard'
      });
      return;
    }

    // Priority 2: Query params (fallback)
    const tourId = this.route.snapshot.queryParamMap.get('tourId');
    const tourMotardId = this.route.snapshot.queryParamMap.get('tourMotardId');

    if (tourId && tourMotardId) {
      // Minimal data from query params
      this.tourSummary.set({
        tourId,
        tourMotardId,
        title: this.route.snapshot.queryParamMap.get('title') || 'Tour',
        startAt: this.route.snapshot.queryParamMap.get('startAt') || '',
        startLocationText: this.route.snapshot.queryParamMap.get('startLocation') || '',
        endLocationText: this.route.snapshot.queryParamMap.get('endLocation') || '',
        pricePerSeat: Number(this.route.snapshot.queryParamMap.get('price')) || 0,
        motardName: this.route.snapshot.queryParamMap.get('motardName') || 'Motard'
      });
      return;
    }

    // Error: No tour data provided
    this.errorMessage.set('Missing tour information. Please select a tour first.');
    setTimeout(() => {
      this.router.navigate(['/tours']);
    }, 3000);
  }

  openSafetyRulesModal(): void {
    import('./safety-rules-modal.component').then(m => {
      this.dialog.open(m.SafetyRulesModalComponent, {
        width: '600px',
        maxHeight: '80vh'
      });
    });
  }

  onSubmit(): void {
    if (this.bookingForm.invalid) {
      // Mark all fields as touched to show validation errors
      Object.keys(this.bookingForm.controls).forEach(key => {
        this.bookingForm.get(key)?.markAsTouched();
      });
      return;
    }

    const tour = this.tourSummary();
    if (!tour) {
      this.errorMessage.set('Tour information is missing.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const formValue = this.bookingForm.value;

    // Combine emergency contact name and phone
    const emergencyContact = `${formValue.emergencyContact} - ${formValue.emergencyPhone}`;

    const request: CreateBookingRequest = {
      tourId: tour.tourId,
      tourMotardId: tour.tourMotardId,
      emergencyContact,
      costSharingOptIn: formValue.costSharingOptIn || false,
      messageToMotard: formValue.messageToMotard?.trim() || undefined
    };

    this.bookingsService.createBookingRequest(request).subscribe({
      next: (response) => {
        // Success: Navigate to booking detail page
        this.router.navigate(['/bookings', response.bookingId]);
      },
      error: (error) => {
        // Error: Display message and reset submitting state
        this.errorMessage.set(error || 'Failed to create booking request. Please try again.');
        this.isSubmitting.set(false);
      }
    });
  }

  // Validation helper methods for template
  hasError(field: string, error: string): boolean {
    const control = this.bookingForm.get(field);
    return !!(control?.hasError(error) && control?.touched);
  }

  getErrorMessage(field: string): string {
    const control = this.bookingForm.get(field);

    if (!control || !control.touched) {
      return '';
    }

    if (control.hasError('required')) {
      return `${this.getFieldLabel(field)} is required`;
    }

    if (control.hasError('minlength')) {
      const minLength = control.getError('minlength')?.requiredLength;
      return `${this.getFieldLabel(field)} must be at least ${minLength} characters`;
    }

    if (control.hasError('pattern')) {
      return `${this.getFieldLabel(field)} format is invalid`;
    }

    return 'Invalid input';
  }

  private getFieldLabel(field: string): string {
    const labels: Record<string, string> = {
      emergencyContact: 'Emergency contact name',
      emergencyPhone: 'Emergency contact phone',
      acceptSafetyRules: 'Safety rules acceptance',
      costSharingOptIn: 'Cost-sharing preference',
      messageToMotard: 'Message'
    };
    return labels[field] || field;
  }

  formatDate(dateString: string): string {
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
}
