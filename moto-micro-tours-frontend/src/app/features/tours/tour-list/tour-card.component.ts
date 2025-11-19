import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TourListDto } from '../../../core/models/tour.models';

@Component({
  selector: 'app-tour-card',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule
  ],
  templateUrl: './tour-card.component.html',
  styleUrls: ['./tour-card.component.scss']
})
export class TourCardComponent {
  @Input() tour!: TourListDto;
  isBookmarked = false;

  get availableSeats(): number {
    return this.tour.totalCapacity - this.tour.totalSeatsBooked;
  }

  get statusBadgeClass(): string {
    const available = this.availableSeats;
    if (available === 0) return 'status-full';
    if (available <= 3) return 'status-few';
    return 'status-available';
  }

  get statusText(): string {
    const available = this.availableSeats;
    if (available === 0) return 'Full';
    if (available <= 3) return 'Few seats left';
    return 'Published';
  }

  get daysUntilStart(): number {
    const now = new Date();
    const startDate = new Date(this.tour.startAt);
    const diffTime = startDate.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  }

  get countdownText(): string {
    const days = this.daysUntilStart;
    if (days < 0) return 'Past';
    if (days === 0) return 'Today';
    if (days === 1) return 'Tomorrow';
    return `In ${days} days`;
  }

  get formattedDate(): string {
    return new Date(this.tour.startAt).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  get formattedTime(): string {
    return new Date(this.tour.startAt).toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  toggleBookmark(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.isBookmarked = !this.isBookmarked;
    // TODO: Implement bookmark API call
  }
}
