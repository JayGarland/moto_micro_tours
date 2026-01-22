import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { extractErrorMessage } from '../../shared/utils';

/**
 * Global HTTP error interceptor
 * Standardizes all HTTP error responses to return user-friendly messages
 *
 * This interceptor:
 * - Catches all HTTP errors
 * - Extracts user-friendly messages from backend error responses
 * - Returns consistent string error messages
 * - Prevents "[object Object]" being displayed to users
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Extract user-friendly message using centralized utility
      const message = extractErrorMessage(error, 'An unexpected error occurred');

      // Log errors for debugging (can be removed in production)
      if (error.status >= 500) {
        console.error('Server error:', error);
      } else if (error.status === 0) {
        console.error('Network error:', error);
      }

      // Return the error message as a string
      return throwError(() => message);
    })
  );
};
