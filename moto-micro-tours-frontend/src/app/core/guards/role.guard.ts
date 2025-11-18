import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard = (requiredRole: 'Motard' | 'Participant'): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.getIsAuthenticated()) {
      router.navigate(['/auth/login'], {
        queryParams: { returnUrl: state.url }
      });
      return false;
    }

    const currentUser = authService.getCurrentUser();
    if (currentUser && currentUser.roles.includes(requiredRole)) {
      return true;
    }

    // Redirect to unauthorized page or home
    router.navigate(['/unauthorized']);
    return false;
  };
};
