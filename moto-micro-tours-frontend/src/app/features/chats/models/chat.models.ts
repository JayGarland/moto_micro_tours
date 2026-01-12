/**
 * Chat Models - matching backend DTOs
 * Stage F5: Pre-booking chat models
 */

export interface MessageDto {
  id: string;
  senderUserId: string;
  content: string;
  sentAt: string; // ISO date string
  conversationType: 'prebooking' | 'booking';
  conversationId: string;
}

export interface SendPreBookingMessageDto {
  tourId: string;
  motardUserId: string;
  content: string;
}

export interface SendBookingMessageDto {
  bookingId: string;
  content: string;
}

export interface ConversationSummaryDto {
  conversationId: string;
  tourId: string;
  tourTitle: string;
  motardUserId: string;
  motardName: string;
  participantUserId: string;
  participantName: string;
  createdAt: string;
  lastMessage?: string;
  lastMessageSenderId?: string;
  lastMessageTime?: string;
}

export interface BookingConversationSummaryDto {
  conversationId: string;
  bookingId: string;
  tourId: string;
  tourTitle: string;
  motardUserId: string;
  motardName: string;
  participantUserId: string;
  participantName: string;
  bookingStatus: string;
  createdAt: string;
  lastMessage?: string;
  lastMessageSenderId?: string;
  lastMessageTime?: string;
}

/**
 * Response from GET /api/bookings/{id}/conversation endpoint
 */
export interface BookingConversationDto {
  bookingId: string;
  conversationId: string | null;
  motardUserId?: string;
  motardName?: string;
  participantUserId?: string;
  participantName?: string;
}

/**
 * UI-specific message model with additional helper properties
 */
export interface ChatMessage extends MessageDto {
  isOwnMessage?: boolean;
  displayTime?: string;
}

/**
 * UI-enriched conversation summary for inbox display
 */
export interface ConversationListItem extends ConversationSummaryDto {
  otherUserName: string; // Name of the other participant (not current user)
  otherUserId: string; // ID of the other participant
  lastMessage?: string; // Preview of last message content
  lastMessageTime?: string; // ISO string or formatted time
  displayTime?: string; // Formatted for UI (e.g. "2h ago", "Yesterday")
}

/**
 * UI-enriched booking conversation summary for inbox display
 */
export interface BookingConversationListItem extends BookingConversationSummaryDto {
  otherUserName: string; // Name of the other participant (not current user)
  otherUserId: string; // ID of the other participant
  displayTime?: string; // Formatted for UI
}
