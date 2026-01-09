/**
 * Review Target Type enum - matches backend ReviewTargetType
 */
export enum ReviewTargetType {
  Tour = 0,
  User = 1
}

/**
 * DTO for creating a new review
 */
export interface CreateReviewDto {
  bookingId: string;
  targetType: ReviewTargetType;
  targetId: string; // TourId or UserId
  rating: number; // 1-5
  comment?: string;
}

/**
 * DTO for adding a reply to a review
 */
export interface CreateReviewReplyDto {
  message: string;
}

/**
 * DTO for review reply
 */
export interface ReviewReplyDto {
  id: string;
  reviewId: string;
  authorUserId: string;
  authorUsername: string;
  message: string;
  createdAtUtc: string;
}

/**
 * DTO for returning review data with replies
 */
export interface ReviewDto {
  id: string;
  bookingId: string;
  reviewerUserId: string;
  reviewerUsername: string;
  targetType: ReviewTargetType;
  targetId: string;
  rating: number;
  comment?: string;
  createdAtUtc: string;
  replies: ReviewReplyDto[];
}
