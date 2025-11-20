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
import { ChatSidebarComponent } from '../../chats/chat-sidebar/chat-sidebar.component';

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
    TourCardComponent,
    ChatSidebarComponent
  ],
  templateUrl: './motard-profile.component.html',
  styleUrls: ['./motard-profile.component.scss']
})
export class MotardProfileComponent implements OnInit {
  profile: MotardProfileDto | null = null;
  loading = true;
  error: string | null = null;

  // Chat state - Stage F5
  showChat = signal(false);
  selectedTourId = signal<string | null>(null);
  selectedTourTitle = signal<string>('');
  selectedTourDetails = signal<{
    rating?: number;
    tripDate?: string;
    duration?: string;
    priceRange?: string;
    startingCity?: string;
  }>({});

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

  /**
   * Open pre-booking chat - Stage F5
   * Opens chat sidebar with the first upcoming tour (or null if none)
   */
  onStartChat(): void {
    if (!this.profile) return;

    // Use first upcoming tour if available
    const firstTour = this.profile.upcomingTours?.[0];
    
    if (firstTour) {
      this.selectedTourId.set(firstTour.tourId);
      this.selectedTourTitle.set(firstTour.title);
      
      // Format tour details for context bar
      const tripDate = this.formatTripDate(firstTour.startAt);
      const priceRange = `$${firstTour.pricePerSeat}`;
      const startingCity = firstTour.startLocationText;
      
      this.selectedTourDetails.set({
        rating: this.profile.avgRating,
        tripDate,
        priceRange,
        startingCity
      });
    } else {
      // No upcoming tours - still allow general chat
      // Backend will handle conversation without specific tour
      this.selectedTourId.set(null);
      this.selectedTourTitle.set('General Inquiry');
    }

    this.showChat.set(true);
  }

  /**
   * Close chat sidebar
   */
  onCloseChat(): void {
    this.showChat.set(false);
  }
  
  /**
   * Format trip date for display
   * Converts ISO date string to readable format
   */
  private formatTripDate(isoDate: string): string {
    const date = new Date(isoDate);
    const month = date.toLocaleDateString('en-US', { month: 'short' });
    const day = date.getDate();
    const year = date.getFullYear();
    return `${month} ${day}, ${year}`;
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
