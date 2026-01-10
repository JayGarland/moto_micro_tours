import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
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
    return this.apiService.post<BookingResponse>('bookings/request', request);
  }

  /**
   * Get booking by ID (full details)
   * Endpoint: GET /api/bookings/{id}
   * @param bookingId - The booking ID
   * @returns Observable of BookingDetail
   */
  getBookingById(bookingId: string): Observable<BookingDetail> {
    return this.apiService.get<BookingDetail>(`bookings/${bookingId}`);
  }

  /**
   * Get all bookings for the current participant
   * Endpoint: GET /api/bookings
   * @returns Observable array of BookingResponse
   */
  getParticipantBookings(): Observable<BookingResponse[]> {
    return this.apiService.get<BookingResponse[]>('bookings');
  }

  /**
   * Mark booking as paid (mock payment)
   * Endpoint: POST /api/payments/pay/{bookingId}
   * @param bookingId - The booking ID
   * @returns Observable of PaymentConfirmation
   */
  markBookingAsPaid(bookingId: string): Observable<PaymentConfirmation> {
    return this.apiService.post<PaymentConfirmation>(`payments/pay/${bookingId}`, {});
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
    return this.apiService.get<BookingResponse[]>('bookings');
  }

  /**
   * Approve a booking request (motard only)
   * Endpoint: POST /api/bookings/{id}/approve
   * @param bookingId - The booking ID
   * @returns Observable of BookingDetail
   */
  approveBooking(bookingId: string): Observable<BookingDetail> {
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/approve`, {});
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
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/decline`, body);
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
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/cancel`, body);
  }

  /**
   * Refund a paid booking (motard only)
   * Endpoint: POST /api/bookings/{id}/refund
   * @param bookingId - The booking ID
   * @returns Observable of BookingDetail
   */
  refundBooking(bookingId: string): Observable<BookingDetail> {
    return this.apiService.post<BookingDetail>(`bookings/${bookingId}/refund`, {});
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
    return this.apiService.post<BookingResponse>(`bookings/${bookingId}/complete`, {});
  }
}
