import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401 || error.status === 403) {
        // Avoid redirect loops / double-encoding: don't navigate if we're already
        // on the login page (or already redirecting there).
        const currentUrl = router.url || '';
        if (!currentUrl.startsWith('/auth/login')) {
          // Redirect to login on authentication errors, preserving the current URL
          router.navigate(['/auth/login'], {
            queryParams: { returnUrl: currentUrl }
          });
        }
      }
      return throwError(() => error);
    })
  );
};
