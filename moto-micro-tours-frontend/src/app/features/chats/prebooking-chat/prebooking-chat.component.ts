import { Component, Input, OnInit, OnChanges, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { ChatService } from '../services/chat.service';
import { ChatMessage } from '../models/chat.models';
import { MessageListComponent } from '../components/message-list/message-list.component';
import { MessageInputComponent } from '../components/message-input/message-input.component';

/**
 * PrebookingChatComponent - Smart/Container Component
 * Stage F5: Pre-booking chat between participant and motard
 *
 * Responsibilities:
 * - Manage chat state (loading, sending, errors)
 * - Fetch messages from API via ChatService
 * - Send messages via ChatService
 * - Subscribe to message stream (single source of truth)
 * - Handle polling for new messages (auto-refresh)
 * - Navigate to booking request
 *
 * Presentation handled by:
 * - MessageListComponent (dumb)
 * - MessageInputComponent (dumb)
 */
@Component({
  selector: 'app-prebooking-chat',
  standalone: true,
  imports: [CommonModule, MessageListComponent, MessageInputComponent],
  templateUrl: './prebooking-chat.component.html',
  styleUrl: './prebooking-chat.component.scss'
})
export class PrebookingChatComponent implements OnInit, OnChanges, OnDestroy {
  private chatService = inject(ChatService);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  @ViewChild(MessageInputComponent) messageInput?: MessageInputComponent;

  // Input: conversation context from parent
  // Mode 1 (motard profile): tourId + motardUserId required, conversationId optional
  // Mode 2 (inbox): conversationId required, tourId + motardUserId optional
  @Input() tourId?: string;
  @Input() motardUserId?: string;
  @Input() chatWithName?: string; // Name of the person you're chatting with (role-agnostic)
  @Input() conversationId?: string;

  // Tour details for context bar
  @Input() tourTitle?: string;
  @Input() motardRating?: number; // e.g., 4.8
  @Input() tripDate?: string; // e.g., "Mar 15-20, 2025"
  @Input() duration?: string; // e.g., "6 days" // TODO: Calculate from EndAt when available
  @Input() priceRange?: string; // e.g., "$800-1200"
  @Input() startingCity?: string; // e.g., "Paris"
  // TODO: Add tourDescription input when Tour.Description field is added to database

  // State signals
  messages = signal<ChatMessage[]>([]);
  loading = signal(false);
  sending = signal(false);
  loadError = signal<string | null>(null);
  sendError = signal<string | null>(null);

  ngOnInit(): void {
    // Mode 2 (inbox): conversationId is provided
    if (this.conversationId) {
      this.loadMessages();
      return;
    }

    // Mode 1 (motard profile): tourId and motardUserId required
    if (!this.tourId || !this.motardUserId) {
      console.error('PrebookingChatComponent: Either conversationId OR (tourId AND motardUserId) are required');
      this.loadError.set('Invalid chat configuration');
      return;
    }

    this.loadMessages();
  }

  ngOnChanges(): void {
    // If conversationId changes (e.g., parent found existing conversation), reload
    if (this.conversationId) {
      this.loadMessages();
    }
  }

  ngOnDestroy(): void {
    // Stop polling when component is destroyed
    if (this.conversationId) {
      this.chatService.disconnectFromPreBookingChat(this.conversationId);
    }

    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load messages from API and subscribe to the stream
   * If conversationId exists, load that conversation and subscribe to updates
   * Otherwise, messages will be empty until first message is sent
   *
   * This establishes the single source of truth: the service's BehaviorSubject
   * Also starts polling for new messages
   */
  private loadMessages(): void {
    // Stop existing polling if any
    if (this.conversationId) {
      this.chatService.stopPolling(this.conversationId);
    }

    if (!this.conversationId) {
      // No conversation yet - wait for user to send first message
      // Subscribe to empty stream in case conversation gets created
      this.subscribeToMessageStream();
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.loadError.set(null);

    // Load initial messages from API/cache
    this.chatService
      .getPreBookingMessages(this.conversationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (messages) => {
          this.messages.set(messages);
          this.loading.set(false);

          // Now subscribe to the stream for live updates
          this.subscribeToMessageStream();

          // Try SignalR first, fallback to polling if needed
          this.chatService.connectToPreBookingChat(this.conversationId!);
        },
        error: (error) => {
          console.error('Error loading messages:', error);
          this.loadError.set('Failed to load messages. Please try again.');
          this.loading.set(false);
        }
      });
  }

  /**
   * Subscribe to message stream from service (single source of truth)
   * This keeps the UI in sync with the service's cache
   */
  private subscribeToMessageStream(): void {
    if (!this.conversationId) return;

    this.chatService
      .getMessagesStream(this.conversationId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (messages) => {
          this.messages.set(messages);
        }
      });
  }

  /**
   * Send a new message
   * Called when user clicks send in MessageInputComponent
   */
  onSendMessage(content: string): void {
    if (!content.trim() || this.sending()) {
      return;
    }

    // In inbox mode with existing conversation, we need tourId and motardUserId
    // They should be available from the conversation context
    if (!this.tourId || !this.motardUserId) {
      this.sendError.set('Cannot send message: missing tour or motard information');
      return;
    }

    this.sending.set(true);
    this.sendError.set(null);

    this.chatService
      .sendPreBookingMessage({
        tourId: this.tourId,
        motardUserId: this.motardUserId,
        content: content.trim()
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (message) => {
          // Service automatically adds message to cache
          // Update conversationId if this was the first message
          if (!this.conversationId) {
            this.conversationId = message.conversationId;
            // Now subscribe to the stream for this new conversation
            this.subscribeToMessageStream();
            // Try SignalR first, fallback to polling if needed
            this.chatService.connectToPreBookingChat(this.conversationId);
          }

          // Clear input on success
          this.messageInput?.clearInput();
          this.sending.set(false);
          this.sendError.set(null);
        },
        error: (error) => {
          console.error('Error sending message:', error);
          const errorMsg = error?.error?.error || 'Failed to send message. Please try again.';
          this.sendError.set(errorMsg);
          this.sending.set(false);

          // Keep draft in input - don't clear
        }
      });
  }

  /**
   * Navigate to booking request flow
   */
  onRequestBooking(): void {
    if (!this.tourId || !this.motardUserId) {
      console.error('Cannot request booking: missing tourId or motardUserId');
      return;
    }

    // Navigate to booking request with tour context
    this.router.navigate(['/bookings/request'], {
      state: {
        tourId: this.tourId,
        tourMotardId: this.motardUserId,
        tourSummary: {
          title: this.tourTitle || 'Tour',
          startLocation: this.startingCity || '',
          endLocation: '', // Not available in chat context
          date: this.tripDate || '',
          price: 0, // Not available in chat context
          motardName: this.chatWithName || 'Motard',
        }
      },
      queryParams: {
        tourId: this.tourId,
        tourMotardId: this.motardUserId
      }
    });
  }

  /**
   * Close chat
   * Emits close event to parent (who manages the sidebar/modal)
   */
  onClose(): void {
    // Parent component should handle closing the sidebar/modal
    // This will be implemented when we integrate with motard profile
  }
}
