import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest } from '../../../core/models/user.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  loginForm: FormGroup;
  loading = signal(false);
  error = signal<string | null>(null);
  hidePassword = signal(true);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      emailOrUsername: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const credentials: LoginRequest = this.loginForm.value;
    console.log('Attempting login with:', credentials);
    console.log('API URL:', 'http://localhost:5256/api/auth/login');

    this.authService.login(credentials).subscribe({
      next: () => {
        this.loading.set(false);
        // Redirect based on role to new dashboard routes
        if (this.authService.getIsMotard()) {
          this.router.navigate(['/dashboard/motard']);
        } else if (this.authService.getIsParticipant()) {
          this.router.navigate(['/dashboard/participant']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        this.loading.set(false);
        console.error('Login error:', err);
        console.error('Error status:', err.status);
        console.error('Error message:', err.error);
        this.error.set(this.mapLoginError(err));
      }
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }

  // Map server / network errors to friendly, actionable messages for the user
  private mapLoginError(err: any): string {
    // Network or CORS issue
    if (!err || err.status === 0) {
      return 'Network error — please check your internet connection and try again.';
    }

    // If backend returned a structured error with a message, prefer it but map common cases
    const serverMessage = err.error?.message || err.message;

    if (err.status === 401) {
      // Unauthorized — most likely bad credentials
      return 'Invalid email/username or password. Please try again.';
    }

    if (err.status === 423 || (err.error?.code && err.error.code === 'ACCOUNT_LOCKED')) {
      return 'Your account is locked. Check your email for unlock instructions or contact support.';
    }

    if (err.status >= 500) {
      return 'Server error — please try again later.';
    }

    // If server provided field errors (e.g., validation), pick the first helpful message
    const fieldErrors = err.error?.errors || err.error?.fieldErrors;
    if (fieldErrors && typeof fieldErrors === 'object') {
      // fieldErrors could be { fieldName: ['msg1', 'msg2'] }
      for (const key of Object.keys(fieldErrors)) {
        const val = fieldErrors[key];
        if (Array.isArray(val) && val.length) {
          return val[0];
        }
        if (typeof val === 'string' && val.length) {
          return val;
        }
      }
    }

    // If backend gave a specific message, use it (but avoid leaking raw debug objects)
    if (serverMessage && typeof serverMessage === 'string') {
      // Map a few known backend messages to friendlier variants
      const low = serverMessage.toLowerCase();
      if (low.includes('invalid credentials') || low.includes('invalid username') || low.includes('invalid password')) {
        return 'Invalid email/username or password. Please try again.';
      }
      return serverMessage;
    }

    // Generic fallback
    return 'Login failed. Please try again.';
  }
// I can implement a lightweight HTTP error interceptor to centralize this mapping (recommended for consistency across the app). This would move mapLoginError logic to a shared place and let every component call a small helper.
// I can sweep other components to replace literal routerLink="/..." back anchors with Location.back() handlers (or wire them to a future NavigationService).
// I can update login.component.html to show field-level error messages derived from fieldErrors if you want more granular validation feedback.
}
