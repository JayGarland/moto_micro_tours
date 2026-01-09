import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CreateReviewDto, CreateReviewReplyDto, ReviewDto } from '../models/review.models';

@Injectable({
  providedIn: 'root'
})
export class ReviewsService {
  private readonly endpoint = 'reviews';

  constructor(private apiService: ApiService) {}

  /**
   * Create a new review (for tour or user)
   * POST /api/reviews
   */
  createReview(review: CreateReviewDto): Observable<ReviewDto> {
    return this.apiService.post<ReviewDto>(this.endpoint, review);
  }

  /**
   * Add a reply to a review
   * POST /api/reviews/{id}/replies
   */
  addReply(reviewId: string, reply: CreateReviewReplyDto): Observable<ReviewDto> {
    return this.apiService.post<ReviewDto>(`${this.endpoint}/${reviewId}/replies`, reply);
  }

  /**
   * Get review by ID
   * GET /api/reviews/{id}
   */
  getReviewById(reviewId: string): Observable<ReviewDto> {
    return this.apiService.get<ReviewDto>(`${this.endpoint}/${reviewId}`);
  }

  /**
   * Get all reviews for a booking
   * GET /api/bookings/{id}/reviews
   */
  getReviewsByBooking(bookingId: string): Observable<ReviewDto[]> {
    return this.apiService.get<ReviewDto[]>(`bookings/${bookingId}/reviews`);
  }

  /**
   * Get all reviews for a tour
   * GET /api/tours/{id}/reviews
   */
  getReviewsByTour(tourId: string): Observable<ReviewDto[]> {
    return this.apiService.get<ReviewDto[]>(`tours/${tourId}/reviews`);
  }

  /**
   * Get all reviews for a user (motard)
   * GET /api/users/{id}/reviews
   */
  getReviewsByUser(userId: string): Observable<ReviewDto[]> {
    return this.apiService.get<ReviewDto[]>(`users/${userId}/reviews`);
  }
}
