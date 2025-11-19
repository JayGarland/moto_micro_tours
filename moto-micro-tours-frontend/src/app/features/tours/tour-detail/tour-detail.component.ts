import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { ToursService } from '../services/tours.service';
import { TourDetailDto } from '../../../core/models/tour.models';

@Component({
  selector: 'app-tour-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule
  ],
  templateUrl: './tour-detail.component.html',
  styleUrls: ['./tour-detail.component.scss']
})
export class TourDetailComponent implements OnInit {
  tour: TourDetailDto | null = null;
  isLoading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private toursService: ToursService
  ) {}

  ngOnInit(): void {
    const tourId = this.route.snapshot.paramMap.get('id');
    if (tourId) {
      this.loadTour(tourId);
    }
  }

  loadTour(id: string): void {
    this.isLoading = true;
    this.error = null;

    this.toursService.getTourById(id).subscribe({
      next: (tour) => {
        this.tour = tour;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading tour:', error);
        this.error = 'Failed to load tour details. Please try again.';
        this.isLoading = false;
      }
    });
  }

  get formattedDate(): string {
    if (!this.tour) return '';
    return new Date(this.tour.startAt).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  get formattedTime(): string {
    if (!this.tour) return '';
    return new Date(this.tour.startAt).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getAvailableSeats(motard: any): number {
    return motard.capacitySeats - motard.seatsBooked;
  }
}
