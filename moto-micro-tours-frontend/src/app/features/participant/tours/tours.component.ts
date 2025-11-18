import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-tours',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="tours-container">
      <h1>Available Tours</h1>
      <p>Browse and book exciting motorcycle tours. Features coming soon!</p>
    </div>
  `,
  styles: [`
    .tours-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }
  `]
})
export class ToursComponent {}
