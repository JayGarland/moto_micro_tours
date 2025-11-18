import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="dashboard-container">
      <h1>Motard Dashboard</h1>
      <p>Welcome to your motard dashboard. Tour management features coming soon!</p>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }
  `]
})
export class DashboardComponent {}
