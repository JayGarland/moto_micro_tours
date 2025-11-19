import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tour-create',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="tour-create-container">
      <h1>Create New Tour</h1>
      <p>Coming in Stage F9...</p>
    </div>
  `,
  styles: [`
    .tour-create-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 2rem;
    }
  `]
})
export class TourCreateComponent {}
