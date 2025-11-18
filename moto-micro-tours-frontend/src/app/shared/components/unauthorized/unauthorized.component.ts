import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="unauthorized-container">
      <mat-icon class="error-icon">block</mat-icon>
      <h1>Access Denied</h1>
      <p>You don't have permission to access this page.</p>
      <a mat-raised-button color="primary" routerLink="/home">Go Home</a>
    </div>
  `,
  styles: [`
    .unauthorized-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: 2rem;
      text-align: center;

      .error-icon {
        font-size: 5rem;
        width: 5rem;
        height: 5rem;
        color: #f44336;
        margin-bottom: 1rem;
      }

      h1 {
        font-size: 2rem;
        margin: 0 0 1rem 0;
      }

      p {
        color: var(--text-secondary);
        margin: 0 0 2rem 0;
      }
    }
  `]
})
export class UnauthorizedComponent {}
