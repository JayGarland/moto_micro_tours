import { Component, OnInit } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
    private router: Router,
    private toursService: ToursService,
    private location: Location
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

  goBack(): void {
    this.location.back();
  }

  bookNow(motard: any): void {
    if (!this.tour) return;

    // Navigate with state data + query param fallback
    this.router.navigate(['/bookings/request'], {
      state: {
        tourId: this.tour.id,
        tourMotardId: motard.tourMotardId,
        tourSummary: {
          title: this.tour.title,
          startLocation: this.tour.startLocationText,
          endLocation: this.tour.endLocationText,
          date: this.tour.startAt,
          price: motard.pricePerSeat,
          motardName: motard.motardName,
        }
      },
      queryParams: {
        tourId: this.tour.id,
        tourMotardId: motard.tourMotardId
      }
    });
  }
}
