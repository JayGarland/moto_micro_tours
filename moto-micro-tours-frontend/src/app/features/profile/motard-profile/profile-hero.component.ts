import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MotardProfileDto } from '../../../core/models/motard.models';

@Component({
  selector: 'app-profile-hero',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './profile-hero.component.html',
  styleUrls: ['./profile-hero.component.scss']
})
export class ProfileHeroComponent {
  @Input() profile!: MotardProfileDto;
  @Output() startChat = new EventEmitter<void>();
  @Output() bookTour = new EventEmitter<void>();

  onStartChat(): void {
    this.startChat.emit();
  }

  onBookTour(): void {
    this.bookTour.emit();
  }

  /**
   * Generate array for star rating display
   */
  getStarArray(): boolean[] {
    const rating = this.profile.avgRating || 0;
    const fullStars = Math.floor(rating);
    return Array(5).fill(false).map((_, i) => i < fullStars);
  }
}
