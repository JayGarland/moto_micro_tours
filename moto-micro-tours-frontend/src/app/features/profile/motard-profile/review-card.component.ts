import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ReviewDto, ReviewTargetType } from '../../bookings/models/review.models';
import { ReviewsService } from '../../bookings/services/reviews.service';
import { AuthService } from '../../../core/services/auth.service';
import { extractErrorMessage } from '../../../shared/utils';

@Component({
  selector: 'app-review-card',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatSnackBarModule
  ],
  templateUrl: './review-card.component.html',
  styleUrls: ['./review-card.component.scss']
})
export class ReviewCardComponent {
  @Input() review!: ReviewDto;
  @Input() expanded: boolean = false;
  @Input() currentUserId?: string; // Pass from parent to check if user is author

  private readonly reviewsService = inject(ReviewsService);
  private readonly authService = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);

  showReplyForm = false;
  replyMessage = '';
  isSubmittingReply = false;

  /**
   * Check if current user can reply
   * - Reviewer can add updates to their own review
   * - Target user can reply (only for User reviews, not Tour reviews)
   */
  get canReply(): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Reviewer can add updates
    const isReviewer = this.review.reviewerUserId === currentUser.userId;

    // Target can reply (only for User type reviews)
    const isTarget = this.review.targetType === 1 && this.review.targetId === currentUser.userId;

    return isReviewer || isTarget;
  }

  /**
   * Check if current user is the reviewer (for UI text differentiation)
   */
  get isReviewer(): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;
    return this.review.reviewerUserId === currentUser.userId;
  }

  getTargetLabel(): string {
    if (this.review.targetType === ReviewTargetType.Tour) {
      return 'Tour review';
    }

    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return 'User review';

    if (this.review.targetId === currentUser.userId) {
      return 'About you';
    }

    if (this.authService.getIsMotard()) {
      return 'Participant review';
    }

    if (this.authService.getIsParticipant()) {
      return 'Motard review';
    }

    return 'User review';
  }

  getTargetLabelClass(): string {
    return this.review.targetType === ReviewTargetType.Tour ? 'target-tour' : 'target-user';
  }

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

  /**
   * Toggle reply form visibility
   */
  toggleReplyForm(): void {
    this.showReplyForm = !this.showReplyForm;
    if (!this.showReplyForm) {
      this.replyMessage = '';
    }
  }

  /**
   * Submit reply (update) to review
   */
  submitReply(): void {
    if (!this.replyMessage.trim()) {
      this.snackBar.open('Please enter a message', 'Close', { duration: 3000 });
      return;
    }

    this.isSubmittingReply = true;
    this.reviewsService.addReply(this.review.id, {
      message: this.replyMessage.trim()
    }).subscribe({
      next: (updatedReview) => {
        this.review = updatedReview; // Update with new reply
        this.replyMessage = '';
        this.showReplyForm = false;
        this.isSubmittingReply = false;
        this.snackBar.open('Update added successfully', 'Close', { duration: 3000 });
      },
      error: (error) => {
        this.isSubmittingReply = false;
        const errorMsg = extractErrorMessage(error, 'Failed to add update');
        this.snackBar.open(errorMsg, 'Close', { duration: 5000 });
      }
    });
  }

  /**
   * Get sorted replies (chronological order)
   */
  getSortedReplies() {
    if (!this.review.replies) return [];
    return [...this.review.replies].sort((a, b) =>
      new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime()
    );
  }
}
