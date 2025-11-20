import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ChatService } from '../services/chat.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConversationListItem } from '../models/chat.models';
import { ChatSidebarComponent } from '../chat-sidebar/chat-sidebar.component';

/**
 * PrebookingInboxComponent
 * Stage F5: Main inbox page for pre-booking conversations
 * 
 * Responsibilities:
 * - Load all conversations for current user (participant or motard)
 * - Display conversation list with last message preview
 * - Handle conversation selection
 * - Open chat-sidebar for selected conversation
 * - Support both desktop (split view) and mobile (full-screen chat)
 */
@Component({
  selector: 'app-prebooking-inbox',
  standalone: true,
  imports: [CommonModule, ChatSidebarComponent],
  templateUrl: './prebooking-inbox.component.html',
  styleUrl: './prebooking-inbox.component.scss'
})
export class PrebookingInboxComponent implements OnInit, OnDestroy {
  private chatService = inject(ChatService);
  private authService = inject(AuthService);
  private destroy$ = new Subject<void>();

  // State
  conversations = signal<ConversationListItem[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  
  // Selected conversation
  selectedConversationId = signal<string | null>(null);
  selectedConversation = signal<ConversationListItem | null>(null);
  showChat = signal(false);

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
   * Load all conversations for current user
   */
  loadConversations(): void {
    this.loading.set(true);
    this.error.set(null);

    this.chatService
      .getPreBookingConversationsForInbox()
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
          console.error('Error loading conversations:', err);
          this.error.set('Failed to load conversations. Please try again.');
          this.loading.set(false);
        }
      });
  }

  /**
   * Handle conversation selection from list
   */
  onSelectConversation(conversation: ConversationListItem): void {
    this.selectedConversationId.set(conversation.conversationId);
    this.selectedConversation.set(conversation);
    this.showChat.set(true);
  }

  /**
   * Handle chat close
   */
  onCloseChat(): void {
    this.showChat.set(false);
    // Keep selection in list but hide chat
    // Or clear selection: this.selectedConversationId.set(null);
    
    // Optionally reload conversations to update last message
    this.loadConversations();
  }

  /**
   * Check if conversation is currently selected
   */
  isSelected(conversationId: string): boolean {
    return this.selectedConversationId() === conversationId;
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
}
