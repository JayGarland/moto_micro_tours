import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-booking-review-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule
  ],
  templateUrl: './booking-review-form.html',
  styleUrl: './booking-review-form.scss'
})
export class BookingReviewForm {
  @Input() isMotardViewer: boolean = false;
  @Input() motardName: string = '';
  @Input() participantName: string = '';
  @Input() tourTitle: string = '';
  @Input() isSubmitting: boolean = false;
  @Output() submitReviews = new EventEmitter<{
    tourReview: { rating: number; comment: string } | null;
    motardReview: { rating: number; comment: string } | null;
    participantReview: { rating: number; comment: string } | null;
  }>();
  @Output() cancel = new EventEmitter<void>();

  reviewForm: FormGroup;
  tourRating = signal<number>(0);
  motardRating = signal<number>(0);
  participantRating = signal<number>(0);
  hoveredTourRating = signal<number>(0);
  hoveredMotardRating = signal<number>(0);
  hoveredParticipantRating = signal<number>(0);

  constructor(private fb: FormBuilder) {
    this.reviewForm = this.fb.group({
      tourComment: ['', [Validators.maxLength(500)]],
      motardComment: ['', [Validators.maxLength(500)]],
      participantComment: ['', [Validators.maxLength(500)]]
    });
  }

  setTourRating(rating: number): void {
    this.tourRating.set(rating);
  }

  setMotardRating(rating: number): void {
    this.motardRating.set(rating);
  }

  setParticipantRating(rating: number): void {
    this.participantRating.set(rating);
  }

  setHoveredTourRating(rating: number): void {
    this.hoveredTourRating.set(rating);
  }

  setHoveredMotardRating(rating: number): void {
    this.hoveredMotardRating.set(rating);
  }

  setHoveredParticipantRating(rating: number): void {
    this.hoveredParticipantRating.set(rating);
  }

  clearTourHover(): void {
    this.hoveredTourRating.set(0);
  }

  clearMotardHover(): void {
    this.hoveredMotardRating.set(0);
  }

  clearParticipantHover(): void {
    this.hoveredParticipantRating.set(0);
  }

  getTourStarIcon(position: number): string {
    const rating = this.hoveredTourRating() || this.tourRating();
    return position <= rating ? 'star' : 'star_border';
  }

  getMotardStarIcon(position: number): string {
    const rating = this.hoveredMotardRating() || this.motardRating();
    return position <= rating ? 'star' : 'star_border';
  }

  getParticipantStarIcon(position: number): string {
    const rating = this.hoveredParticipantRating() || this.participantRating();
    return position <= rating ? 'star' : 'star_border';
  }

  onSubmit(): void {
    // At least one review must be provided
    if (this.tourRating() === 0 && this.motardRating() === 0 && this.participantRating() === 0) {
      return;
    }

    if (this.reviewForm.valid) {
      this.submitReviews.emit({
        tourReview: this.tourRating() > 0 ? {
          rating: this.tourRating(),
          comment: this.reviewForm.value.tourComment || ''
        } : null,
        motardReview: this.motardRating() > 0 ? {
          rating: this.motardRating(),
          comment: this.reviewForm.value.motardComment || ''
        } : null,
        participantReview: this.participantRating() > 0 ? {
          rating: this.participantRating(),
          comment: this.reviewForm.value.participantComment || ''
        } : null
      });
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
