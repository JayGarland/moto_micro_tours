import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MotardProfileDto } from '../../../core/models/motard.models';

@Component({
  selector: 'app-profile-sidebar',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './profile-sidebar.component.html',
  styleUrls: ['./profile-sidebar.component.scss']
})
export class ProfileSidebarComponent {
  @Input() profile!: MotardProfileDto;
  @Output() startChat = new EventEmitter<void>();

  onStartChat(): void {
    this.startChat.emit();
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
