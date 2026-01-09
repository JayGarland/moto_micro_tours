/**
 * TypeScript models for Motard profiles, reviews, and related DTOs
 * Matching backend DTOs from TourDtos.cs
 */

import { ReviewDto } from '../../features/bookings/models/review.models';

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
 * Parameters for fetching motard profile
 */
export interface MotardProfileParams {
  includeUpcomingTours?: boolean;
}
