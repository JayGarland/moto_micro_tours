import { Component, Input, AfterViewChecked, AfterViewInit, OnChanges, SimpleChanges, ViewChild, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatMessage } from '../../models/chat.models';

/**
 * MessageListComponent - Dumb/Presentational Component
 * Stage F5: Displays a list of chat messages with auto-scroll
 * 
 * Responsibilities:
 * - Display messages in bubbles
 * - Visual distinction between own/other messages
 * - Show timestamps
 * - Auto-scroll to latest message
 * - Display loading spinner
 * 
 * Parent (smart component) handles:
 * - Loading messages from API
 * - Sending messages
 * - State management
 */
@Component({
  selector: 'app-message-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './message-list.component.html',
  styleUrl: './message-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageListComponent implements AfterViewInit, AfterViewChecked, OnChanges {
  @Input() messages: ChatMessage[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;

  @ViewChild('scrollContainer') private scrollContainer?: ElementRef;
  
  private shouldScrollToBottom = false;
  private lastMessageCount = 0;
  private isInitialized = false;

  constructor(private cdr: ChangeDetectorRef) {}

  ngAfterViewInit(): void {
    // Scroll to bottom on initial load
    this.isInitialized = true;
    if (this.messages.length > 0) {
      this.scrollToBottomImmediate();
    }
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottomImmediate();
      this.shouldScrollToBottom = false;
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Scroll when messages array changes (new messages added or initial load)
    if (changes['messages']) {
      const currentMessages = changes['messages'].currentValue as ChatMessage[];
      const previousMessages = changes['messages'].previousValue as ChatMessage[];
      
      // Check if messages were added
      if (currentMessages && 
          (!previousMessages || currentMessages.length > previousMessages.length)) {
        this.shouldScrollToBottom = true;
        this.lastMessageCount = currentMessages.length;
        // Trigger change detection for OnPush strategy
        this.cdr.markForCheck();
      }
    }
  }

  private scrollToBottomImmediate(): void {
    try {
      if (this.scrollContainer) {
        const container = this.scrollContainer.nativeElement;
        // Double requestAnimationFrame for more reliable scrolling
        requestAnimationFrame(() => {
          requestAnimationFrame(() => {
            container.scrollTop = container.scrollHeight;
          });
        });
      }
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }

  /**
   * Track messages by ID for performance
   */
  trackByMessageId(index: number, message: ChatMessage): string {
    return message.id;
  }
}
