import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError, interval, Subscription, of } from 'rxjs';
import { tap, catchError, map, switchMap, startWith } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';
import {
  MessageDto,
  SendPreBookingMessageDto,
  SendBookingMessageDto,
  ConversationSummaryDto,
  BookingConversationSummaryDto,
  BookingConversationDto,
  ChatMessage,
  ConversationListItem
} from '../models/chat.models';

/**
 * ChatService - Stage F7.2 Phase 2
 * Handles pre-booking and booking conversations with SignalR support
 *
 * Features:
 * - Send/receive messages via REST + SignalR
 * - In-memory caching with BehaviorSubject per conversation
 * - Real-time updates via SignalR for booking chats
 * - Automatic polling fallback for pre-booking chats
 * - Lightweight error handling (errors bubble up)
 *
 * Architecture (Hybrid):
 * - Sending: REST API (reliable)
 * - Receiving: SignalR (real-time) with polling fallback
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

  // SignalR connection for booking chats
  private hubConnection: signalR.HubConnection | null = null;
  private connectedBookingIds = new Set<string>();

  // SignalR connection for pre-booking chats
  private preBookingHubConnection: signalR.HubConnection | null = null;
  private connectedPreBookingIds = new Set<string>();

  // Polling interval in milliseconds (default: 3 seconds)
  private readonly POLLING_INTERVAL = 3000;

  private readonly lastViewedStorageKey = 'prebookingLastViewed';
  private lastViewed = new Map<string, number>();

  constructor() {
    this.loadLastViewed();
  }

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
          let lastMessage: string | undefined = conv.lastMessage;
          let lastMessageTime: string | undefined = conv.lastMessageTime;
          let lastMessageSenderId: string | undefined = conv.lastMessageSenderId;

          if (this.messagesCache.has(conv.conversationId)) {
            const messages = this.messagesCache.get(conv.conversationId)!.value;
            if (messages.length > 0) {
              const last = messages[messages.length - 1];
              lastMessage = last.content;
              lastMessageTime = last.sentAt;
              lastMessageSenderId = last.senderUserId;
            }
          }

          return {
            ...conv,
            otherUserName,
            otherUserId,
            lastMessage,
            lastMessageSenderId,
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
   * Persist last viewed timestamp for a pre-booking conversation
   */
  markPreBookingConversationRead(conversationId: string, lastMessageTime?: string): void {
    const timestamp = lastMessageTime ? new Date(lastMessageTime).getTime() : Date.now();
    this.lastViewed.set(conversationId, timestamp);
    this.saveLastViewed();
  }

  /**
   * Check if a pre-booking conversation has unread messages for current user
   */
  isPreBookingConversationUnread(conversation: ConversationListItem): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;
    if (conversation.lastMessageSenderId === currentUser.userId) return false;

    const lastMessageTime = conversation.lastMessageTime || conversation.createdAt;
    const lastViewed = this.lastViewed.get(conversation.conversationId) || 0;
    return new Date(lastMessageTime).getTime() > lastViewed;
  }

  /**
   * Polls unread status for pre-booking conversations
   */
  getPreBookingUnreadStatus(pollMs: number = 30000): Observable<boolean> {
    return interval(pollMs).pipe(
      startWith(0),
      switchMap(() => this.getPreBookingConversationsForInbox()),
      map(conversations => conversations.some(conv => this.isPreBookingConversationUnread(conv))),
      catchError((error) => {
        console.error('Error checking unread conversations:', error);
        return of(false);
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
   * Get booking conversation info by bookingId (F7.2 Phase 1 helper)
   * Returns conversation details or null if no conversation exists yet
   */
  getBookingConversation(bookingId: string): Observable<BookingConversationDto> {
    return this.apiService.get<BookingConversationDto>(`bookings/${bookingId}/conversation`).pipe(
      catchError((error) => {
        console.error('Error fetching booking conversation:', error);
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

  private loadLastViewed(): void {
    try {
      const raw = localStorage.getItem(this.lastViewedStorageKey);
      if (!raw) return;
      const parsed = JSON.parse(raw) as Record<string, number>;
      Object.entries(parsed).forEach(([key, value]) => {
        if (typeof value === 'number') {
          this.lastViewed.set(key, value);
        }
      });
    } catch (error) {
      console.warn('Failed to load last viewed state:', error);
    }
  }

  private saveLastViewed(): void {
    try {
      const payload: Record<string, number> = {};
      this.lastViewed.forEach((value, key) => {
        payload[key] = value;
      });
      localStorage.setItem(this.lastViewedStorageKey, JSON.stringify(payload));
    } catch (error) {
      console.warn('Failed to save last viewed state:', error);
    }
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

  /**
   * Initialize SignalR connection for booking chats (F7.2 Phase 2)
   * Connection is shared across all booking conversations
   */
  private async initializeSignalR(): Promise<void> {
    if (this.hubConnection) {
      return; // Already initialized
    }

    // Build SignalR hub URL from environment API URL
    const hubUrl = environment.apiUrl.replace('/api', '/hubs/booking-chat');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        withCredentials: true // Include cookies for authentication
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Listen for incoming messages
    this.hubConnection.on('ReceiveMessage', (message: MessageDto) => {
      console.log('[SignalR] Received message:', message);
      this.addMessageToCache(message.conversationId, message);
    });

    try {
      await this.hubConnection.start();
      console.log('[SignalR] Connected to booking chat hub');
    } catch (error) {
      console.error('[SignalR] Connection failed:', error);
      // Fallback to polling will be used
    }
  }

  /**
   * Initialize SignalR connection for pre-booking chats
   */
  private async initializePreBookingSignalR(): Promise<void> {
    if (this.preBookingHubConnection) {
      return; // Already initialized
    }

    const hubUrl = environment.apiUrl.replace('/api', '/hubs/prebooking-chat');

    this.preBookingHubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.preBookingHubConnection.on('ReceiveMessage', (message: MessageDto) => {
      console.log('[SignalR] Received pre-booking message:', message);
      this.addMessageToCache(message.conversationId, message);
    });

    try {
      await this.preBookingHubConnection.start();
      console.log('[SignalR] Connected to pre-booking chat hub');
    } catch (error) {
      console.error('[SignalR] Pre-booking connection failed:', error);
    }
  }

  /**
   * Join a booking's SignalR group to receive real-time messages
   */
  async connectToBookingChat(bookingId: string, conversationId: string): Promise<void> {
    await this.initializeSignalR();

    if (this.hubConnection?.state === signalR.HubConnectionState.Connecting) {
      const connected = await this.waitForHubConnection(this.hubConnection);
      if (!connected) {
        console.warn('[SignalR] Not connected, falling back to polling');
        this.startPolling(conversationId, 'booking');
        return;
      }
    }

    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      console.warn('[SignalR] Not connected, falling back to polling');
      // Start polling as fallback
      this.startPolling(conversationId, 'booking');
      return;
    }

    try {
      await this.hubConnection.invoke('JoinBookingGroup', bookingId);
      this.connectedBookingIds.add(bookingId);
      console.log(`[SignalR] Joined booking group: ${bookingId}`);

      // Stop polling since we have SignalR
      this.stopPolling(conversationId);
    } catch (error) {
      console.error('[SignalR] Failed to join booking group:', error);
      // Fallback to polling
      this.startPolling(conversationId, 'booking');
    }
  }

  /**
   * Join a pre-booking SignalR group to receive real-time messages
   */
  async connectToPreBookingChat(conversationId: string): Promise<void> {
    await this.initializePreBookingSignalR();

    if (this.preBookingHubConnection?.state === signalR.HubConnectionState.Connecting) {
      const connected = await this.waitForHubConnection(this.preBookingHubConnection);
      if (!connected) {
        console.warn('[SignalR] Pre-booking not connected, falling back to polling');
        this.startPolling(conversationId, 'prebooking');
        return;
      }
    }

    if (!this.preBookingHubConnection || this.preBookingHubConnection.state !== signalR.HubConnectionState.Connected) {
      console.warn('[SignalR] Pre-booking not connected, falling back to polling');
      this.startPolling(conversationId, 'prebooking');
      return;
    }

    try {
      await this.preBookingHubConnection.invoke('JoinPreBookingGroup', conversationId);
      this.connectedPreBookingIds.add(conversationId);
      console.log(`[SignalR] Joined pre-booking group: ${conversationId}`);

      // Stop polling since we have SignalR
      this.stopPolling(conversationId);
    } catch (error) {
      console.error('[SignalR] Failed to join pre-booking group:', error);
      this.startPolling(conversationId, 'prebooking');
    }
  }

  /**
   * Leave a booking's SignalR group
   */
  async disconnectFromBookingChat(bookingId: string, conversationId: string): Promise<void> {
    if (!this.hubConnection || !this.connectedBookingIds.has(bookingId)) {
      return;
    }

    try {
      await this.hubConnection.invoke('LeaveBookingGroup', bookingId);
      this.connectedBookingIds.delete(bookingId);
      console.log(`[SignalR] Left booking group: ${bookingId}`);
    } catch (error) {
      console.error('[SignalR] Failed to leave booking group:', error);
    }

    // Stop polling too
    this.stopPolling(conversationId);
  }

  /**
   * Leave a pre-booking SignalR group
   */
  async disconnectFromPreBookingChat(conversationId: string): Promise<void> {
    if (!this.preBookingHubConnection || !this.connectedPreBookingIds.has(conversationId)) {
      return;
    }

    try {
      await this.preBookingHubConnection.invoke('LeavePreBookingGroup', conversationId);
      this.connectedPreBookingIds.delete(conversationId);
      console.log(`[SignalR] Left pre-booking group: ${conversationId}`);
    } catch (error) {
      console.error('[SignalR] Failed to leave pre-booking group:', error);
    }

    this.stopPolling(conversationId);
  }

  /**
   * Disconnect SignalR connection (call on service destroy or logout)
   */
  async disconnectSignalR(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
      this.connectedBookingIds.clear();
      console.log('[SignalR] Disconnected');
    }

    if (this.preBookingHubConnection) {
      await this.preBookingHubConnection.stop();
      this.preBookingHubConnection = null;
      this.connectedPreBookingIds.clear();
      console.log('[SignalR] Disconnected pre-booking');
    }
  }

  /**
   * Wait briefly for a SignalR connection to transition out of Connecting.
   */
  private async waitForHubConnection(connection: signalR.HubConnection, timeoutMs = 3000): Promise<boolean> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
      if (connection.state === signalR.HubConnectionState.Connected) {
        return true;
      }
      if (connection.state === signalR.HubConnectionState.Disconnected) {
        return false;
      }
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    return connection.state === signalR.HubConnectionState.Connected;
  }
}
