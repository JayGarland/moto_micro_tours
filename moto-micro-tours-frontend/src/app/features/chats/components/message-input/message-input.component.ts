import { Component, EventEmitter, Output, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/**
 * MessageInputComponent - Dumb/Presentational Component
 * Stage F5: Input box for sending messages
 * 
 * Responsibilities:
 * - Display input box and send button
 * - Emit message content when sent
 * - Disable button while sending
 * - Keep draft in memory if send fails
 * - Show inline error
 * 
 * Parent (smart component) handles:
 * - Actually sending the message via API
 * - Managing sending state
 * - Error handling
 */
@Component({
  selector: 'app-message-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './message-input.component.html',
  styleUrl: './message-input.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageInputComponent {
  @Input() sending = false;
  @Input() error: string | null = null;
  @Input() placeholder = 'Type a message...';
  
  @Output() sendMessage = new EventEmitter<string>();

  messageContent = '';

  onSend(): void {
    const content = this.messageContent.trim();
    
    if (!content || this.sending) {
      return;
    }

    // Emit the message to parent
    this.sendMessage.emit(content);
    
    // Clear input only after successful send
    // Parent will call clearInput() after success
  }

  /**
   * Clear input after successful send
   * Called by parent component
   */
  clearInput(): void {
    this.messageContent = '';
  }

  /**
   * Handle Enter key to send message
   * Shift+Enter for new line (handled by textarea)
   */
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSend();
    }
  }
}
