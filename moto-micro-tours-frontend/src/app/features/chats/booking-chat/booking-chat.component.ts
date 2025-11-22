import { Component, Input, OnInit, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ChatService } from '../services/chat.service';
import { ChatMessage } from '../models/chat.models';
import { MessageListComponent } from '../components/message-list/message-list.component';
import { MessageInputComponent } from '../components/message-input/message-input.component';

/**
 * BookingChatComponent - Smart/Container Component
 * Stage F7.2 Phase 2: Booking chat with SignalR real-time messaging
 *
 * Responsibilities:
 * - Manage chat state (loading, sending, errors)
 * - Fetch messages from API via ChatService
 * - Send messages via ChatService (REST)
 * - Connect to SignalR for real-time message delivery
 * - Subscribe to message stream (single source of truth)
 * - Handle SignalR connection lifecycle
 *
 * Presentation handled by:
 * - MessageListComponent (dumb)
 * - MessageInputComponent (dumb)
 */
@Component({
  selector: 'app-booking-chat',
  standalone: true,
  imports: [CommonModule, MessageListComponent, MessageInputComponent],
  templateUrl: './booking-chat.component.html',
  styleUrl: './booking-chat.component.scss'
})
export class BookingChatComponent implements OnInit, OnDestroy {
  private chatService = inject(ChatService);
  private destroy$ = new Subject<void>();

  @ViewChild(MessageInputComponent) messageInput?: MessageInputComponent;

  // Input: booking context from parent (booking detail component)
  @Input({ required: true }) bookingId!: string;
  @Input() chatWithName?: string; // Name of the other person (motard or participant)
  @Input() tourTitle?: string;
  @Input() bookingStatus?: string;

  // Internal state
  conversationId = signal<string | null>(null);
  messages = signal<ChatMessage[]>([]);
  loading = signal(false);
  sending = signal(false);
  loadError = signal<string | null>(null);
  sendError = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.bookingId) {
      console.error('BookingChatComponent: bookingId is required');
      this.loadError.set('Invalid chat configuration');
      return;
    }

    this.loadConversation();
  }

  ngOnDestroy(): void {
    // Disconnect from SignalR and stop polling when component is destroyed
    const convId = this.conversationId();
    if (convId) {
      this.chatService.disconnectFromBookingChat(this.bookingId, convId);
    }

    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load conversation info and messages
   * First gets conversationId from bookingId, then loads messages if conversation exists
   */
  private loadConversation(): void {
    this.loading.set(true);
    this.loadError.set(null);

    // Get conversation info from booking
    this.chatService
      .getBookingConversation(this.bookingId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (conversationInfo) => {
          if (conversationInfo.conversationId) {
            // Conversation exists, load messages
            this.conversationId.set(conversationInfo.conversationId);
            this.loadMessages();
          } else {
            // No conversation yet - will be created when first message is sent
            this.loading.set(false);
            this.subscribeToMessageStream(); // Subscribe to empty stream
          }
        },
        error: (error) => {
          console.error('Error loading conversation:', error);
          const errorMsg = error?.error?.error || 'Failed to load conversation. Please try again.';
          this.loadError.set(errorMsg);
          this.loading.set(false);
        }
      });
  }

  /**
   * Load messages from API and connect to SignalR for real-time updates
   */
  private loadMessages(): void {
    const convId = this.conversationId();
    if (!convId) {
      this.loading.set(false);
      return;
    }

    this.chatService
      .getBookingMessages(convId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (messages) => {
          this.messages.set(messages);
          this.loading.set(false);

          // Subscribe to stream for live updates
          this.subscribeToMessageStream();

          // Connect to SignalR for real-time messaging (with polling fallback)
          this.chatService.connectToBookingChat(this.bookingId, convId);
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
    const convId = this.conversationId();
    if (!convId) return;

    this.chatService
      .getMessagesStream(convId)
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

    this.sending.set(true);
    this.sendError.set(null);

    this.chatService
      .sendBookingMessage({
        bookingId: this.bookingId,
        content: content.trim()
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (message) => {
          // Service automatically adds message to cache
          // Update conversationId if this was the first message
          if (!this.conversationId()) {
            this.conversationId.set(message.conversationId);
            // Now subscribe to the stream for this new conversation
            this.subscribeToMessageStream();
            // Connect to SignalR for real-time updates
            this.chatService.connectToBookingChat(this.bookingId, message.conversationId);
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
}
