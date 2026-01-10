/**
 * Extract user-friendly error message from any error object
 * Handles various error response formats from backend
 *
 * @param error - The error object from HTTP call or other source
 * @param fallback - Default message if no error message found
 * @returns User-friendly error message string
 */
export function extractErrorMessage(error: any, fallback: string = 'An error occurred'): string {
  // Already a string
  if (typeof error === 'string') {
    return error;
  }

  // HttpErrorResponse structure - backend returns { error: "message" }
  if (error?.error?.error) {
    return error.error.error;
  }

  // Alternative backend structure - { message: "message" }
  if (error?.error?.message) {
    return error.error.message;
  }

  // Direct string error in error property
  if (typeof error?.error === 'string') {
    return error.error;
  }

  // Generic error object with message property
  if (error?.message) {
    return error.message;
  }

  // HTTP status-based fallback messages
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
        return fallback;
    }
  }

  return fallback;
}

