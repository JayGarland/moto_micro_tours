import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const bookingsRoutes: Routes = [
  {
    path: 'request',
    loadComponent: () =>
      import('./booking-request/booking-request-form/booking-request-form').then(
        m => m.BookingRequestForm
      ),
    canActivate: [authGuard],
    title: 'Request Booking'
  },
  {
    path: ':bookingId',
    loadComponent: () =>
      import('./booking-detail/booking-detail/booking-detail').then(
        m => m.BookingDetail
      ),
    canActivate: [authGuard],
    title: 'Booking Details'
  },
  {
    path: ':bookingId/pay',
    loadComponent: () =>
      import('./payment/payment-form/payment-form').then(
        m => m.PaymentForm
      ),
    canActivate: [authGuard],
    title: 'Payment'
  }
];
