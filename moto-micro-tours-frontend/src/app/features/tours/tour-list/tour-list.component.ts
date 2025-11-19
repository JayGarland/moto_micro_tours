import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourFilterComponent } from './tour-filter.component';
import { TourCardComponent } from './tour-card.component';
import { ToursService } from '../services/tours.service';
import { TourListDto, TourFilters } from '../../../core/models/tour.models';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-tour-list',
  standalone: true,
  imports: [
    CommonModule,
    TourFilterComponent,
    TourCardComponent,
    MatProgressSpinnerModule
  ],
  templateUrl: './tour-list.component.html',
  styleUrls: ['./tour-list.component.scss']
})
export class TourListComponent implements OnInit {
  tours: TourListDto[] = [];
  isLoading = false;
  error: string | null = null;
  currentFilters: TourFilters = {};

  constructor(private toursService: ToursService) {}

  ngOnInit(): void {
    this.loadTours();
  }

  loadTours(filters?: TourFilters): void {
    this.isLoading = true;
    this.error = null;
    this.currentFilters = filters || {};

    this.toursService.getTours(filters).subscribe({
      next: (tours) => {
        this.tours = tours;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading tours:', error);
        this.error = 'Failed to load tours. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onFilterChange(filters: TourFilters): void {
    this.loadTours(filters);
  }
}
