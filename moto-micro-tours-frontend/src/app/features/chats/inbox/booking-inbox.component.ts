import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { ChatService } from '../services/chat.service';
import { AuthService } from '../../../core/services/auth.service';
import { BookingConversationListItem } from '../models/chat.models';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

/**
 * BookingInboxComponent
 * Option B: Inbox for booking conversations (post-approval/paid)
 * 
 * Responsibilities:
 * - Load all booking conversations for current user (participant or motard)
 * - Display conversation list with booking status, last message preview
 * - Navigate to booking detail page for chat access
 */
@Component({
  selector: 'app-booking-inbox',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatChipsModule],
  templateUrl: './booking-inbox.component.html',
  styleUrl: './booking-inbox.component.scss'
})
export class BookingInboxComponent implements OnInit, OnDestroy {
  private chatService = inject(ChatService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  // State
  conversations = signal<BookingConversationListItem[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // Current user
  currentUser = this.authService.getCurrentUser();

  ngOnInit(): void {
    this.loadConversations();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all booking conversations for current user
   */
  loadConversations(): void {
    this.loading.set(true);
    this.error.set(null);

    this.chatService
      .getBookingConversationsForInbox()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (conversations) => {
          // Sort by last message time or creation time (newest first)
          const sorted = conversations.sort((a, b) => {
            const timeA = new Date(a.lastMessageTime || a.createdAt).getTime();
            const timeB = new Date(b.lastMessageTime || b.createdAt).getTime();
            return timeB - timeA; // Descending
          });
          
          this.conversations.set(sorted);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error loading booking conversations:', err);
          this.error.set('Failed to load booking conversations. Please try again.');
          this.loading.set(false);
        }
      });
  }

  /**
   * Navigate to booking detail page
   */
  onSelectConversation(conversation: BookingConversationListItem): void {
    this.router.navigate(['/bookings', conversation.bookingId]);
  }

  /**
   * Truncate message for preview
   */
  truncateMessage(message: string | undefined, maxLength: number = 50): string {
    if (!message) return 'No messages yet';
    return message.length > maxLength 
      ? message.substring(0, maxLength) + '...' 
      : message;
  }

  /**
   * Get status badge color
   */
  getStatusColor(status: string): string {
    switch (status) {
      case 'Paid':
        return 'primary';
      case 'Approved':
        return 'accent';
      case 'Cancelled':
      case 'CancelledExpired':
        return 'warn';
      case 'Refunded':
        return 'warn';
      default:
        return '';
    }
  }
}
