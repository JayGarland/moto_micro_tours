import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { ReviewDto } from '../../../core/models/motard.models';

@Component({
  selector: 'app-review-card',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './review-card.component.html',
  styleUrls: ['./review-card.component.scss']
})
export class ReviewCardComponent {
  @Input() review!: ReviewDto;
  @Input() expanded: boolean = false;

  /**
   * Generate array for star rating display
   */
  getStarArray(): boolean[] {
    const fullStars = Math.floor(this.review.rating);
    return Array(5).fill(false).map((_, i) => i < fullStars);
  }

  /**
   * Get truncated comment (2-3 lines)
   */
  getTruncatedComment(): string {
    if (!this.review.comment) return '';
    const maxLength = 120;
    if (this.review.comment.length <= maxLength || this.expanded) {
      return this.review.comment;
    }
    return this.review.comment.substring(0, maxLength) + '...';
  }

  /**
   * Check if comment needs truncation
   */
  needsTruncation(): boolean {
    return !!this.review.comment && this.review.comment.length > 120;
  }
}
