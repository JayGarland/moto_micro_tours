/**
 * Booking status enum - matches backend BookingStatus
 */
export enum BookingStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Declined = 'Declined',
  Cancelled = 'Cancelled',
  CancelledExpired = 'CancelledExpired',
  Paid = 'Paid',
  Refunded = 'Refunded'
}

/**
 * Payment status enum - matches backend PaymentStatus
 */
export enum PaymentStatus {
  Initiated = 'Initiated',
  Succeeded = 'Succeeded',
  Failed = 'Failed',
  Refunded = 'Refunded'
}

/**
 * Payout status enum - matches backend PayoutStatus
 */
export enum PayoutStatus {
  Pending = 'Pending',
  Settled = 'Settled',
  Failed = 'Failed'
}
