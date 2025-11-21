import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatCardModule],
  template: `
    <div class="landing-page">
      <div class="hero-section">
        <h1 class="hero-title">Moto Micro Tours</h1>
        <p class="hero-subtitle tagline">Your Ultimate Motorcycle Adventure Platform</p>
        <div class="hero-actions cta-buttons">
          <a mat-raised-button color="primary" routerLink="/auth/register">Get Started</a>
          <a mat-stroked-button routerLink="/auth/login" class="login-button">Sign In</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* Landing page uses full blue hero - defined globally in styles.scss */
    /* This component only needs layout-specific styles */

    .landing-page {
      /* Already styled globally, just ensure proper layout */
      align-items: center;
      justify-content: center;
    }

    .hero-section {
      text-align: center;
      padding: 3rem 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .hero-title {
      font-size: 3rem;
      font-weight: 700;
      margin: 0 0 1rem 0;
    }

    .hero-subtitle {
      font-size: 1.25rem;
      margin: 0 0 2rem 0;
      opacity: 0.95;
    }

    .hero-actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
    }

    .login-button {
      background-color: #ffffff !important;
      color: #0b3c8a !important;
      border-color: #ffffff !important;

      &:hover {
        background-color: rgba(255, 255, 255, 0.9) !important;
      }
    }

    @media (max-width: 600px) {
      .hero-title {
        font-size: 2rem;
      }

      .hero-subtitle {
        font-size: 1rem;
      }

      .hero-section {
        padding: 2rem 1rem;
      }
    }
  `]
})
export class HomeComponent {}
