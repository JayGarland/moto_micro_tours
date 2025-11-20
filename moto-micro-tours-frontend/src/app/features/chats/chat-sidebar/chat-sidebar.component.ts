import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { trigger, state, style, transition, animate } from '@angular/animations';
import { PrebookingChatComponent } from '../prebooking-chat/prebooking-chat.component';
import { ChatService } from '../services/chat.service';

/**
 * ChatSidebarComponent - Sidebar/Drawer for Chat
 * Stage F5: Slides in from right on desktop, full-screen on mobile
 * 
 * Responsibilities:
 * - Show/hide animation
 * - Overlay backdrop
 * - Close button
 * - Host the PrebookingChatComponent
 * - Support two modes:
 *   1. Find/create conversation (from motard profile)
 *   2. Direct conversation ID (from inbox)
 */
@Component({
  selector: 'app-chat-sidebar',
  standalone: true,
  imports: [CommonModule, PrebookingChatComponent],
  templateUrl: './chat-sidebar.component.html',
  styleUrl: './chat-sidebar.component.scss',
  animations: [
    trigger('slideIn', [
      state('void', style({ transform: 'translateX(100%)' })),
      state('*', style({ transform: 'translateX(0)' })),
      transition(':enter', animate('300ms ease-out')),
      transition(':leave', animate('300ms ease-in'))
    ]),
    trigger('fadeIn', [
      state('void', style({ opacity: 0 })),
      state('*', style({ opacity: 1 })),
      transition(':enter', animate('200ms ease-out')),
      transition(':leave', animate('200ms ease-in'))
    ])
  ]
})
export class ChatSidebarComponent implements OnInit, OnChanges {
  private chatService = inject(ChatService);

  // Inputs from parent
  @Input() isOpen = false;
  
  // Mode 1: From motard profile (find/create conversation)
  @Input() tourId?: string;
  @Input() tourTitle = 'Tour';
  @Input() motardUserId?: string;
  @Input() chatWithName?: string; // Name of person you're chatting with
  
  // Mode 2: From inbox (direct conversation ID)
  @Input() conversationId?: string;
  
  // Tour details for context bar (optional)
  @Input() motardRating?: number;
  @Input() tripDate?: string;
  @Input() duration?: string;
  @Input() priceRange?: string;
  @Input() startingCity?: string;

  // Output to notify parent to close
  @Output() close = new EventEmitter<void>();

  // Conversation state
  internalConversationId?: string;
  loadingConversation = false;
  conversationError: string | null = null;

  ngOnInit(): void {
    if (this.isOpen) {
      this.initializeConversation();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // When sidebar opens (or reopens)
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.initializeConversation();
      } else {
        // When closing, reset conversation state for next open
        this.internalConversationId = undefined;
        this.conversationError = null;
      }
    }
    
    // If tour/motard/conversationId changes while open, re-initialize
    if ((changes['tourId'] || changes['motardUserId'] || changes['conversationId']) && this.isOpen) {
      this.initializeConversation();
    }
  }

  /**
   * Initialize conversation based on mode:
   * - If conversationId is provided (inbox mode), use it directly
   * - Otherwise, find/create conversation (motard profile mode)
   */
  initializeConversation(): void {
    // Mode 2: Direct conversation ID from inbox
    if (this.conversationId) {
      this.internalConversationId = this.conversationId;
      this.loadingConversation = false;
      return;
    }

    // Mode 1: Find/create from motard profile
    if (this.tourId && this.motardUserId) {
      this.findOrCreateConversation();
    }
  }

  /**
   * Find existing conversation or prepare for new one
   * Backend creates conversation on first message send
   * Only used in motard profile mode (Mode 1)
   */
  findOrCreateConversation(): void {
    this.loadingConversation = true;
    this.conversationError = null;

    // Try to find existing conversation
    this.chatService.getPreBookingConversations().subscribe({
      next: (conversations) => {
        // Find conversation with this motard and tour
        const existing = conversations.find(
          c => c.motardUserId === this.motardUserId && c.tourId === this.tourId
        );

        if (existing) {
          this.internalConversationId = existing.conversationId;
        }
        // If no existing conversation, internalConversationId stays undefined
        // Backend will create it when first message is sent

        this.loadingConversation = false;
      },
      error: (error) => {
        console.error('Error fetching conversations:', error);
        this.conversationError = 'Failed to load conversation';
        this.loadingConversation = false;
      }
    });
  }

  /**
   * Close sidebar
   */
  onClose(): void {
    this.close.emit();
  }

  /**
   * Close on backdrop click
   */
  onBackdropClick(): void {
    this.onClose();
  }

  /**
   * Prevent click events from bubbling to backdrop
   */
  onSidebarClick(event: Event): void {
    event.stopPropagation();
  }
}
