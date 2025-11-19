import { Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { LoginRequest, LoginResponse, CurrentUser } from '../models/user.models';
import { Observable, of } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly currentUser = signal<CurrentUser | null>(null);
  private readonly isAuthenticated = signal(false);
  private readonly isMotard = signal(false);
  private readonly isParticipant = signal(false);

  constructor(private apiService: ApiService) {}

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.apiService.post<LoginResponse>('auth/login', credentials).pipe(
      tap((response) => {
        const user: CurrentUser = {
          userId: response.userId,
          username: response.username,
          email: response.email,
          roles: response.roles
        };
        this.currentUser.set(user);
        this.isAuthenticated.set(true);
        this.isMotard.set(user.roles.includes('Motard'));
        this.isParticipant.set(user.roles.includes('Participant'));
      })
    );
  }

  logout(): Observable<void> {
    return this.apiService.post<void>('auth/logout', {}).pipe(
      tap(() => {
        this.currentUser.set(null);
        this.isAuthenticated.set(false);
        this.isMotard.set(false);
        this.isParticipant.set(false);
      })
    );
  }

  getCurrentUser(): CurrentUser | null {
    return this.currentUser();
  }

  getIsAuthenticated(): boolean {
    return this.isAuthenticated();
  }

  getIsMotard(): boolean {
    return this.isMotard();
  }

  getIsParticipant(): boolean {
    return this.isParticipant();
  }

  /**
   * Check authentication by calling the backend current-user endpoint.
   * Returns the current user object or null if not authenticated.
   */
  checkAuth(): Observable<CurrentUser | null> {
    return this.apiService.get<CurrentUser>('auth/me').pipe(
      tap((user) => {
        if (user) {
          this.currentUser.set(user);
          this.isAuthenticated.set(true);
          this.isMotard.set(user.roles?.includes('Motard'));
          this.isParticipant.set(user.roles?.includes('Participant'));
        }
      }),
      catchError((err) => {
        // On error (401 or network), ensure client state is cleared and return null
        this.currentUser.set(null);
        this.isAuthenticated.set(false);
        this.isMotard.set(false);
        this.isParticipant.set(false);
        return of(null);
      })
    );
  }
}
