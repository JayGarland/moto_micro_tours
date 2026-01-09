import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { ReviewDto } from '../../bookings/models/review.models';
import { ReviewCardComponent } from './review-card.component';

@Component({
  selector: 'app-review-list',
  standalone: true,
  imports: [CommonModule, MatButtonModule, ReviewCardComponent],
  templateUrl: './review-list.component.html',
  styleUrls: ['./review-list.component.scss']
})
export class ReviewListComponent {
  @Input() reviews: ReviewDto[] = [];

  displayCount = 5;

  get displayedReviews(): ReviewDto[] {
    return this.reviews.slice(0, this.displayCount);
  }

  get hasMore(): boolean {
    return this.reviews.length > this.displayCount;
  }

  loadMore(): void {
    this.displayCount += 5;
  }
}
