import { BookingStatus, PaymentStatus, PayoutStatus } from './booking-status.enum';

// Re-export enums for convenience
export { BookingStatus, PaymentStatus, PayoutStatus };

/**
 * Request DTO for creating a new booking
 */
export interface CreateBookingRequest {
  tourId: string;
  tourMotardId: string;
  emergencyContact: string;
  costSharingOptIn: boolean;
}

/**
 * Response DTO after creating a booking
 */
export interface BookingResponse {
  bookingId: string;
  tourId: string;
  tourTitle: string;
  tourMotardId: string;
  motardName: string;
  participantId: string;
  status: BookingStatus;
  costSharingOptIn: boolean;
  emergencyContact: string;
  createdAt: string;
  approvedAt?: string;
  expiresAt?: string;
}

/**
 * Payment information DTO
 */
export interface PaymentInfo {
  paymentId: string;
  amount: number;
  status: PaymentStatus;
  providerRef?: string;
  paidAt?: string;
}

/**
 * Payout information DTO
 */
export interface PayoutInfo {
  payoutId: string;
  platformShare: number;
  motardShare: number;
  status: PayoutStatus;
  settledAt?: string;
}

/**
 * Detailed booking information including tour and payment details
 */
export interface BookingDetail {
  bookingId: string;

  // Tour information
  tourId: string;
  tourTitle: string;
  tourStartAt: string;
  startLocationText: string;
  endLocationText: string;

  // Motard information
  tourMotardId: string;
  motardUserId: string;
  motardName: string;
  pricePerSeat: number;

  // Participant information
  participantId: string;
  participantName: string;

  // Booking details
  status: BookingStatus;
  costSharingOptIn: boolean;
  emergencyContact: string;
  createdAt: string;
  approvedAt?: string;
  expiresAt?: string;
  paidAt?: string;
  cancelledAt?: string;
  cancellationReason?: string;

  // Payment information (if exists)
  payment?: PaymentInfo;

  // Payout information (if exists)
  payout?: PayoutInfo;
}

/**
 * Payment confirmation response after successful payment
 */
export interface PaymentConfirmation {
  success: boolean;
  message: string;
  bookingId: string;
  bookingStatus: BookingStatus;
  paidAt?: string;
  payment?: PaymentInfo;
  payout?: PayoutInfo;
  tourTitle: string;
  motardName: string;
}

/**
 * Summary DTO for booking lists
 */
export interface BookingSummary {
  bookingId: string;
  tourTitle: string;
  motardName: string;
  participantName: string;
  tourStartAt: string;
  status: BookingStatus;
  pricePerSeat: number;
  createdAt: string;
  expiresAt?: string;
}
