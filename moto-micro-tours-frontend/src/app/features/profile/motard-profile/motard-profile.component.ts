import { Component, OnInit, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MotardsService } from '../services/motards.service';
import { MotardProfileDto } from '../../../core/models/motard.models';
import { ProfileSidebarComponent } from './profile-sidebar.component';
import { ProfileHeroComponent } from './profile-hero.component';
import { ReviewListComponent } from './review-list.component';
import { FAQSectionComponent } from './faq-section.component';
import { TourCardComponent } from '../../tours/tour-list/tour-card.component';

@Component({
  selector: 'app-motard-profile',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    ProfileSidebarComponent,
    ProfileHeroComponent,
    ReviewListComponent,
    FAQSectionComponent,
    TourCardComponent
  ],
  templateUrl: './motard-profile.component.html',
  styleUrls: ['./motard-profile.component.scss']
})
export class MotardProfileComponent implements OnInit {
  profile: MotardProfileDto | null = null;
  loading = true;
  error: string | null = null;

  // Responsive behavior
  isMobile = signal(false);
  completedToursToShow = signal(3);

  // Computed properties
  displayedCompletedTours = computed(() => {
    const tours = this.profile?.completedTours?.slice(0, this.completedToursToShow()) || [];
    // Map MotardTourSummaryDto to TourListDto format for tour-card component
    return tours.map(tour => ({
      id: tour.tourId,
      title: tour.title,
      startAt: tour.startAt,
      startLocationText: tour.startLocationText,
      endLocationText: tour.endLocationText,
      status: 'Closed',
      notes: '',
      totalCapacity: tour.availableSeats,
      totalSeatsBooked: tour.availableSeats, // All seats were filled since it's completed
      minPrice: tour.pricePerSeat,
      maxPrice: tour.pricePerSeat,
      motardCount: 1,
      motards: []
    }));
  });

  hasMoreCompletedTours = computed(() => {
    return (this.profile?.completedTours?.length || 0) > this.completedToursToShow();
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private motardsService: MotardsService
  ) {
    this.checkScreenSize();
  }

  @HostListener('window:resize')
  onResize() {
    this.checkScreenSize();
  }

  private checkScreenSize() {
    this.isMobile.set(window.innerWidth <= 768);
  }

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('userId');
    if (userId) {
      this.loadProfile(userId);
    } else {
      this.error = 'Invalid user ID';
      this.loading = false;
    }
  }

  loadProfile(userId: string): void {
    this.loading = true;
    this.error = null;

    this.motardsService.getMotardProfile(userId, { includeUpcomingTours: true })
      .subscribe({
        next: (profile: MotardProfileDto) => {
          this.profile = profile;
          this.loading = false;
        },
        error: (err: any) => {
          this.error = 'Failed to load profile';
          this.loading = false;
          console.error('Error loading profile:', err);
        }
      });
  }

  onStartChat(): void {
    // TODO: Navigate to chat or open chat dialog (Stage F5)
    console.log('Start chat with motard:', this.profile?.userId);
    alert('Chat functionality will be implemented in Stage F5');
  }

  onBookTour(): void {
    // Navigate to tours list or scroll to upcoming tours
    const toursSection = document.getElementById('upcoming-tours');
    if (toursSection) {
      toursSection.scrollIntoView({ behavior: 'smooth' });
    }
  }

  goBack(): void {
    this.router.navigate(['/tours']);
  }

  showAllCompletedTours(): void {
    // Show all completed tours by updating the signal
    this.completedToursToShow.set(this.profile?.completedTours?.length || 0);
  }

  onViewCompletedTourDetails(tourId: string): void {
    // TODO: Navigate to tour details or implement completed tour modal
    console.log('View completed tour details:', tourId);
  }
}
