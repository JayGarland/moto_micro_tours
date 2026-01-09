import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReviewsService } from '../../bookings/services/reviews.service';
import { ReviewDto } from '../../bookings/models/review.models';
import { ReviewCardComponent } from '../motard-profile/review-card.component';

@Component({
  selector: 'app-participant-profile',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    ReviewCardComponent
  ],
  templateUrl: './participant-profile.component.html',
  styleUrls: ['./participant-profile.component.scss']
})
export class ParticipantProfileComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly reviewsService = inject(ReviewsService);

  participantId = signal<string>('');
  participantName = signal<string>('Participant');
  reviews = signal<ReviewDto[]>([]);
  isLoading = signal(true);
  isLoadingReviews = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('userId');
    if (id) {
      this.participantId.set(id);
      this.loadParticipantReviews(id);
    } else {
      this.error.set('Invalid participant ID');
      this.isLoading.set(false);
    }
  }

  loadParticipantReviews(userId: string): void {
    this.isLoading.set(true);
    this.isLoadingReviews.set(true);

    this.reviewsService.getReviewsByUser(userId).subscribe({
      next: (reviews: ReviewDto[]) => {
        this.reviews.set(reviews);
        // For now use generic name - could fetch user details from API in future
        if (reviews.length > 0) {
          this.participantName.set('Participant');
        }
        this.isLoading.set(false);
        this.isLoadingReviews.set(false);
      },
      error: (error: any) => {
        console.error('Error loading participant reviews:', error);
        this.error.set('Failed to load participant profile');
        this.isLoading.set(false);
        this.isLoadingReviews.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  get averageRating(): number {
    const reviews = this.reviews();
    if (reviews.length === 0) return 0;
    const sum = reviews.reduce((acc, review) => acc + review.rating, 0);
    return sum / reviews.length;
  }

  get reviewCount(): number {
    return this.reviews().length;
  }
}
