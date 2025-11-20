import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-role-aware-dashboard',
  standalone: true,
  template: '<div class="loading">Loading dashboard...</div>',
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
export class RoleAwareDashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    
    if (!user) {
      this.router.navigate(['/auth/login']);
      return;
    }

    // Route based on user role
    if (user.roles?.includes('Motard')) {
      this.router.navigate(['/dashboard/motard']);
    } else if (user.roles?.includes('Participant')) {
      this.router.navigate(['/dashboard/participant']);
    } else {
      // Default fallback - redirect to home
      this.router.navigate(['/home']);
    }
  }
}
