import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from '../../../core/services/api.service';
import {
  CreateBookingRequest,
  BookingResponse,
  BookingDetail,
  PaymentConfirmation
} from '../models/booking.model';

@Injectable({
  providedIn: 'root',
})
export class BookingsService {
  private readonly apiService = inject(ApiService);

  /**
   * Create a new booking request
   * Endpoint: POST /api/bookings/request
   * @param request - Booking request data
   * @returns Observable of BookingResponse
   */
  createBookingRequest(request: CreateBookingRequest): Observable<BookingResponse> {
    return this.apiService.post<BookingResponse>('bookings/request', request)
      .pipe(
        catchError(error => {
          console.error('Error creating booking request:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Get booking by ID (full details)
   * Endpoint: GET /api/bookings/{id}
   * @param bookingId - The booking ID
   * @returns Observable of BookingDetail
   */
  getBookingById(bookingId: string): Observable<BookingDetail> {
    return this.apiService.get<BookingDetail>(`bookings/${bookingId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching booking details:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Get all bookings for the current participant
   * Endpoint: GET /api/bookings
   * @returns Observable array of BookingResponse
   */
  getParticipantBookings(): Observable<BookingResponse[]> {
    return this.apiService.get<BookingResponse[]>('bookings')
      .pipe(
        catchError(error => {
          console.error('Error fetching participant bookings:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Mark booking as paid (mock payment)
   * Endpoint: POST /api/payments/pay/{bookingId}
   * @param bookingId - The booking ID
   * @returns Observable of PaymentConfirmation
   */
  markBookingAsPaid(bookingId: string): Observable<PaymentConfirmation> {
    return this.apiService.post<PaymentConfirmation>(`payments/pay/${bookingId}`, {})
      .pipe(
        catchError(error => {
          console.error('Error processing payment:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  // ============================================
  // Motard-specific methods (F7+ - Placeholders)
  // ============================================

  /**
   * Get booking requests for motard to approve/decline
   * Endpoint: GET /api/bookings (filtered by role)
   * Returns BookingResponse[] which now includes all fields needed for dashboard
   * @returns Observable array of BookingResponse (used as BookingSummary)
   */
  getMotardBookingRequests(): Observable<BookingResponse[]> {
    return this.apiService.get<BookingResponse[]>('bookings')
      .pipe(
        catchError(error => {
          console.error('Error fetching motard booking requests:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Approve a booking request (motard only)
   * Endpoint: POST /api/bookings/{id}/approve
   * @param bookingId - The booking ID
   * @returns Observable of BookingDetail
   */
  approveBooking(bookingId: string): Observable<BookingDetail> {
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/approve`, {})
      .pipe(
        catchError(error => {
          console.error('Error approving booking:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Decline a booking request (motard only)
   * Endpoint: POST /api/bookings/{id}/decline
   * @param bookingId - The booking ID
   * @param reason - Optional decline reason
   * @returns Observable of BookingDetail
   */
  declineBooking(bookingId: string, reason?: string): Observable<BookingDetail> {
    const body = reason ? { reason } : {};
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/decline`, body)
      .pipe(
        catchError(error => {
          console.error('Error declining booking:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Cancel a booking
   * Endpoint: POST /api/bookings/{id}/cancel
   * @param bookingId - The booking ID
   * @param reason - Optional cancellation reason
   * @returns Observable of BookingDetail
   */
  cancelBooking(bookingId: string, reason?: string): Observable<BookingDetail> {
    const body = reason ? { reason } : {};
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/cancel`, body)
      .pipe(
        catchError(error => {
          console.error('Error cancelling booking:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Refund a paid booking (motard only)
   * Endpoint: POST /api/bookings/{id}/refund
   * @param bookingId - The booking ID
   * @returns Observable of BookingDetail
   */
  refundBooking(bookingId: string): Observable<BookingDetail> {
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/refund`, {})
      .pipe(
        catchError(error => {
          console.error('Error refunding booking:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  /**
   * Mark booking as completed (F8 - Completion Handshake)
   * Endpoint: POST /api/bookings/{id}/complete
   * - Participant or motard marks their side as complete
   * - When both sides complete, booking is fully completed
   * @param bookingId - The booking ID
   * @returns Observable of BookingResponse with updated completion timestamps
   */
  completeBooking(bookingId: string): Observable<BookingResponse> {
    return this.apiService.post<BookingResponse>(`bookings/${bookingId}/complete`, {})
      .pipe(
        catchError(error => {
          console.error('Error completing booking:', error);
          return throwError(() => this.extractErrorMessage(error));
        })
      );
  }

  // ============================================
  // Private Helper Methods
  // ============================================

  /**
   * Extract user-friendly error message from HTTP error response
   * @param error - The error object from HTTP call
   * @returns User-friendly error message
   */
  private extractErrorMessage(error: any): string {
    // Check for backend error message
    if (error?.error?.error) {
      return error.error.error;
    }

    // Check for message field
    if (error?.error?.message) {
      return error.error.message;
    }

    // Check for string error
    if (typeof error?.error === 'string') {
      return error.error;
    }

    // HTTP status-based messages
    if (error?.status) {
      switch (error.status) {
        case 400:
          return 'Invalid request. Please check your input.';
        case 401:
          return 'You must be logged in to perform this action.';
        case 403:
          return 'You do not have permission to perform this action.';
        case 404:
          return 'The requested resource was not found.';
        case 409:
          return 'This action conflicts with existing data.';
        case 500:
          return 'Server error. Please try again later.';
        default:
          return `An error occurred (${error.status}). Please try again.`;
      }
    }

    // Network error
    if (error?.message?.includes('Http failure')) {
      return 'Network error. Please check your connection.';
    }

    // Fallback
    return error?.message || 'An unexpected error occurred. Please try again.';
  }
}
