import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError, interval, Subscription } from 'rxjs';
import { tap, catchError, map, switchMap } from 'rxjs/operators';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  MessageDto,
  SendPreBookingMessageDto,
  SendBookingMessageDto,
  ConversationSummaryDto,
  BookingConversationSummaryDto,
  ChatMessage,
  ConversationListItem
} from '../models/chat.models';

/**
 * ChatService - Stage F5
 * Handles pre-booking and booking conversations
 * 
 * Features:
 * - Send/receive messages
 * - In-memory caching with BehaviorSubject per conversation
 * - Automatic polling for new messages (configurable interval)
 * - Lightweight error handling (errors bubble up)
 * 
 * TODO Phase 2: Add SignalR for real-time messaging
 * TODO Phase 2: Add typing indicator support
 * TODO Phase 2: Implement more sophisticated caching with invalidation
 */
@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiService = inject(ApiService);
  private authService = inject(AuthService);

  // In-memory cache: conversationId -> BehaviorSubject<MessageDto[]>
  private messagesCache = new Map<string, BehaviorSubject<MessageDto[]>>();

  // Loading state per conversation
  private loadingStates = new Map<string, BehaviorSubject<boolean>>();

  // Polling subscriptions: conversationId -> Subscription
  private pollingSubscriptions = new Map<string, Subscription>();

  // Polling interval in milliseconds (default: 3 seconds)
  private readonly POLLING_INTERVAL = 3000;

  /**
   * Send a pre-booking message
   * Creates conversation automatically if it doesn't exist
   */
  sendPreBookingMessage(dto: SendPreBookingMessageDto): Observable<MessageDto> {
    return this.apiService.post<MessageDto>('chats/prebooking/send', dto).pipe(
      tap((message) => {
        // Update cache with new message
        this.addMessageToCache(message.conversationId, message);
      }),
      catchError((error) => {
        console.error('Error sending pre-booking message:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Send a booking message
   */
  sendBookingMessage(dto: SendBookingMessageDto): Observable<MessageDto> {
    return this.apiService.post<MessageDto>('chats/booking/send', dto).pipe(
      tap((message) => {
        this.addMessageToCache(message.conversationId, message);
      }),
      catchError((error) => {
        console.error('Error sending booking message:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Get all pre-booking conversations for current user
   */
  getPreBookingConversations(): Observable<ConversationSummaryDto[]> {
    return this.apiService.get<ConversationSummaryDto[]>('chats/prebooking/conversations').pipe(
      catchError((error) => {
        console.error('Error fetching pre-booking conversations:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Get enriched conversation list for inbox display
   * Adds: other user name/id, last message preview, formatted time
   */
  getPreBookingConversationsForInbox(): Observable<ConversationListItem[]> {
    return this.getPreBookingConversations().pipe(
      map((conversations) => {
        const currentUser = this.authService.getCurrentUser();
        if (!currentUser) return [];

        return conversations.map(conv => {
          // Determine who the "other" user is
          const isMotard = conv.motardUserId === currentUser.userId;
          const otherUserName = isMotard ? conv.participantName : conv.motardName;
          const otherUserId = isMotard ? conv.participantUserId : conv.motardUserId;

          // Check cache for last message
          let lastMessage: string | undefined;
          let lastMessageTime: string | undefined;
          
          if (this.messagesCache.has(conv.conversationId)) {
            const messages = this.messagesCache.get(conv.conversationId)!.value;
            if (messages.length > 0) {
              const last = messages[messages.length - 1];
              lastMessage = last.content;
              lastMessageTime = last.sentAt;
            }
          }

          return {
            ...conv,
            otherUserName,
            otherUserId,
            lastMessage,
            lastMessageTime,
            displayTime: lastMessageTime 
              ? this.formatMessageTime(lastMessageTime)
              : this.formatMessageTime(conv.createdAt)
          };
        });
      })
    );
  }

  /**
   * Get all booking conversations for current user
   */
  getBookingConversations(): Observable<BookingConversationSummaryDto[]> {
    return this.apiService.get<BookingConversationSummaryDto[]>('chats/booking/conversations').pipe(
      catchError((error) => {
        console.error('Error fetching booking conversations:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Get messages for a pre-booking conversation
   * Uses cache if available, otherwise fetches from API
   */
  getPreBookingMessages(conversationId: string, forceRefresh = false): Observable<ChatMessage[]> {
    // Check cache first
    if (!forceRefresh && this.messagesCache.has(conversationId)) {
      return this.messagesCache.get(conversationId)!.asObservable().pipe(
        map(messages => this.enrichMessages(messages))
      );
    }

    // Set loading state
    this.setLoadingState(conversationId, true);

    return this.apiService
      .get<MessageDto[]>(`chats/prebooking/${conversationId}/messages`)
      .pipe(
        tap((messages) => {
          // Initialize or update cache
          if (!this.messagesCache.has(conversationId)) {
            this.messagesCache.set(conversationId, new BehaviorSubject<MessageDto[]>(messages));
          } else {
            this.messagesCache.get(conversationId)!.next(messages);
          }
          this.setLoadingState(conversationId, false);
        }),
        map(messages => this.enrichMessages(messages)),
        catchError((error) => {
          console.error('Error fetching pre-booking messages:', error);
          this.setLoadingState(conversationId, false);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get messages for a booking conversation
   */
  getBookingMessages(conversationId: string, forceRefresh = false): Observable<ChatMessage[]> {
    if (!forceRefresh && this.messagesCache.has(conversationId)) {
      return this.messagesCache.get(conversationId)!.asObservable().pipe(
        map(messages => this.enrichMessages(messages))
      );
    }

    this.setLoadingState(conversationId, true);

    return this.apiService
      .get<MessageDto[]>(`chats/booking/${conversationId}/messages`)
      .pipe(
        tap((messages) => {
          if (!this.messagesCache.has(conversationId)) {
            this.messagesCache.set(conversationId, new BehaviorSubject<MessageDto[]>(messages));
          } else {
            this.messagesCache.get(conversationId)!.next(messages);
          }
          this.setLoadingState(conversationId, false);
        }),
        map(messages => this.enrichMessages(messages)),
        catchError((error) => {
          console.error('Error fetching booking messages:', error);
          this.setLoadingState(conversationId, false);
          return throwError(() => error);
        })
      );
  }

  /**
   * Get cached messages as Observable
   * Returns existing BehaviorSubject for real-time updates
   */
  getMessagesStream(conversationId: string): Observable<ChatMessage[]> {
    if (!this.messagesCache.has(conversationId)) {
      this.messagesCache.set(conversationId, new BehaviorSubject<MessageDto[]>([]));
    }
    return this.messagesCache.get(conversationId)!.asObservable().pipe(
      map(messages => this.enrichMessages(messages))
    );
  }

  /**
   * Get loading state for a conversation
   */
  getLoadingState(conversationId: string): Observable<boolean> {
    if (!this.loadingStates.has(conversationId)) {
      this.loadingStates.set(conversationId, new BehaviorSubject<boolean>(false));
    }
    return this.loadingStates.get(conversationId)!.asObservable();
  }

  /**
   * Clear cache for a specific conversation
   */
  clearConversationCache(conversationId: string): void {
    this.messagesCache.delete(conversationId);
    this.loadingStates.delete(conversationId);
  }

  /**
   * Clear all cached data
   */
  clearAllCache(): void {
    this.messagesCache.clear();
    this.loadingStates.clear();
  }

  /**
   * Start polling for new messages in a pre-booking conversation
   * Automatically fetches new messages every POLLING_INTERVAL milliseconds
   * Updates the cached BehaviorSubject when new messages arrive
   * 
   * @param conversationId The conversation to poll
   */
  startPolling(conversationId: string, type: 'prebooking' | 'booking' = 'prebooking'): void {
    // Stop existing polling if any
    this.stopPolling(conversationId);

    // Create polling subscription
    const polling$ = interval(this.POLLING_INTERVAL).pipe(
      switchMap(() => {
        const endpoint = type === 'prebooking' 
          ? `chats/prebooking/${conversationId}/messages`
          : `chats/booking/${conversationId}/messages`;
        
        return this.apiService.get<MessageDto[]>(endpoint);
      }),
      catchError((error) => {
        console.error(`Polling error for conversation ${conversationId}:`, error);
        // Don't stop polling on errors - continue trying
        return throwError(() => error);
      })
    );

    // Subscribe and update cache
    const subscription = polling$.subscribe({
      next: (messages) => {
        // Update cache with new messages
        if (!this.messagesCache.has(conversationId)) {
          this.messagesCache.set(conversationId, new BehaviorSubject<MessageDto[]>(messages));
        } else {
          const subject = this.messagesCache.get(conversationId)!;
          const currentMessages = subject.value;
          
          // Only update if there are new messages
          if (messages.length > currentMessages.length) {
            subject.next(messages);
          }
        }
      },
      error: (error) => {
        console.error('Polling subscription error:', error);
      }
    });

    this.pollingSubscriptions.set(conversationId, subscription);
  }

  /**
   * Stop polling for a specific conversation
   * 
   * @param conversationId The conversation to stop polling
   */
  stopPolling(conversationId: string): void {
    const subscription = this.pollingSubscriptions.get(conversationId);
    if (subscription) {
      subscription.unsubscribe();
      this.pollingSubscriptions.delete(conversationId);
    }
  }

  /**
   * Stop all active polling
   * Call this when service is destroyed or user logs out
   */
  stopAllPolling(): void {
    this.pollingSubscriptions.forEach((subscription) => subscription.unsubscribe());
    this.pollingSubscriptions.clear();
  }

  /**
   * Check if polling is active for a conversation
   */
  isPolling(conversationId: string): boolean {
    return this.pollingSubscriptions.has(conversationId);
  }

  // ==================== Private Helper Methods ====================

  /**
   * Add a new message to cache
   */
  private addMessageToCache(conversationId: string, message: MessageDto): void {
    if (!this.messagesCache.has(conversationId)) {
      this.messagesCache.set(conversationId, new BehaviorSubject<MessageDto[]>([message]));
    } else {
      const subject = this.messagesCache.get(conversationId)!;
      const currentMessages = subject.value;
      
      // Check if message already exists (prevent duplicates)
      const exists = currentMessages.some(m => m.id === message.id);
      if (!exists) {
        subject.next([...currentMessages, message]);
      }
    }
  }

  /**
   * Enrich messages with UI-specific properties
   */
  private enrichMessages(messages: MessageDto[]): ChatMessage[] {
    const currentUser = this.authService.getCurrentUser();
    
    return messages.map(msg => ({
      ...msg,
      isOwnMessage: msg.senderUserId === currentUser?.userId,
      displayTime: this.formatMessageTime(msg.sentAt)
    }));
  }

  /**
   * Format message timestamp for display
   */
  private formatMessageTime(isoDate: string): string {
    const date = new Date(isoDate);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    
    // Format as date
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  /**
   * Set loading state for a conversation
   */
  private setLoadingState(conversationId: string, loading: boolean): void {
    if (!this.loadingStates.has(conversationId)) {
      this.loadingStates.set(conversationId, new BehaviorSubject<boolean>(loading));
    } else {
      this.loadingStates.get(conversationId)!.next(loading);
    }
  }
}
