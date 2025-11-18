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
    <div class="home-container">
      <div class="hero-section">
        <h1>Moto Micro Tours</h1>
        <p class="tagline">Your Ultimate Motorcycle Adventure Platform</p>
        <div class="cta-buttons">
          <a mat-raised-button color="primary" routerLink="/auth/register">Get Started</a>
          <a mat-stroked-button routerLink="/auth/login">Sign In</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .home-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, var(--yamaha-primary) 0%, var(--yamaha-accent) 100%);
    }

    .hero-section {
      text-align: center;
      color: white;
      padding: 2rem;

      h1 {
        font-size: 3rem;
        font-weight: 700;
        margin: 0 0 1rem 0;
      }

      .tagline {
        font-size: 1.25rem;
        margin: 0 0 2rem 0;
        opacity: 0.9;
      }

      .cta-buttons {
        display: flex;
        gap: 1rem;
        justify-content: center;
        flex-wrap: wrap;
      }
    }

    @media (max-width: 600px) {
      .hero-section h1 {
        font-size: 2rem;
      }
    }
  `]
})
export class HomeComponent {}
