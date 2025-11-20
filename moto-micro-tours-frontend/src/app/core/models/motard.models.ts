/**
 * TypeScript models for Motard profiles, reviews, and related DTOs
 * Matching backend DTOs from TourDtos.cs
 */

/**
 * Complete motard profile with reviews and statistics
 */
export interface MotardProfileDto {
  userId: string;
  userName: string;
  email?: string;

  // Profile details
  bio?: string;
  bikeModel?: string;
  licenseNo?: string;

  // Statistics
  avgRating?: number;
  totalReviews: number;

  // Reviews from past participants
  reviews: ReviewDto[];

  // Optional: Tours this motard is part of (upcoming)
  upcomingTours: MotardTourSummaryDto[];

  // Optional: Completed tours list (same type as upcoming tours)
  completedTours?: MotardTourSummaryDto[];
}

/**
 * Review left by a participant for a motard
 */
export interface ReviewDto {
  id: string;
  authorId: string;
  authorName: string;
  rating: number; // 1-5
  comment?: string;
  createdAt: string; // ISO date string
  bookingId: string;
}

/**
 * Summary of a tour that a motard is part of
 */
export interface MotardTourSummaryDto {
  tourId: string;
  title: string;
  startAt: string; // ISO date string
  startLocationText: string;
  endLocationText: string;
  availableSeats: number;
  pricePerSeat: number;
}

/**
 * Request to create a review for a motard
 */
export interface CreateReviewDto {
  bookingId: string;
  rating: number; // 1-5
  comment?: string;
}

/**
 * Parameters for fetching motard profile
 */
export interface MotardProfileParams {
  includeUpcomingTours?: boolean;
}
