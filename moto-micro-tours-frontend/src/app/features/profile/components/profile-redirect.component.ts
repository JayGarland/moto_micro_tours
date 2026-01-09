import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

/**
 * Profile redirect component that routes to the appropriate profile based on user role
 * This allows a single /profile route that works for all users
 */
@Component({
  selector: 'app-profile-redirect',
  standalone: true,
  template: '<div class="loading">Loading profile...</div>',
  styles: [`
    .loading {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 50vh;
      font-size: 1.125rem;
      color: var(--color-text-secondary, #6b7280);
    }
  `]
})
export class ProfileRedirectComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();

    if (!user) {
      this.router.navigate(['/auth/login']);
      return;
    }

    // For now, redirect to motard profile if user is a motard
    // Otherwise redirect to participant profile
    if (user.roles?.includes('Motard')) {
      this.router.navigate(['/motards', user.userId]);
    } else {
      this.router.navigate(['/participants', user.userId]);
    }
  }
}
