# Stage F5 Testing Checklist
**Pre-booking Chat - Manual Testing Guide**

## Architecture Verification

### ✅ State Ownership
- [ ] **Motard profile owns `showChat` state** - Sidebar only receives `isOpen` input
- [ ] **Sidebar emits `close` event** - Parent (motard profile) handles closing
- [ ] **No independent open/close** - Sidebar is stateless regarding open/close

### ✅ Single Source of Truth
- [ ] **ChatService holds message arrays** - Components subscribe, don't copy
- [ ] **BehaviorSubject per conversationId** - Service maintains cache
- [ ] **Components use signals from service stream** - No local mutation

### ✅ Conversation Identity
- [ ] **Conversation per (rider, motard, tour)** - Key is all three
- [ ] **Sidebar passes identifiers down** - tourId, motardUserId, tourTitle
- [ ] **PrebookingChat determines conversationId** - Orchestrates ChatService

---

## Manual Testing Scenarios

### 1️⃣ First Contact with a Motard

**Steps:**
1. Navigate to a motard profile you've never chatted with
2. Click "Start Chat" button (hero section, sidebar, or mobile FAB)
3. Observe sidebar opens with loading state
4. Wait for empty conversation to load

**Expected Results:**
- ✅ Sidebar slides in from right (full-screen on mobile)
- ✅ No existing messages shown
- ✅ "Start the conversation!" empty state visible
- ✅ Message input is enabled

**Action:**
5. Type a message: "Hi! I'm interested in your tour."
6. Click Send

**Expected Results:**
- ✅ Send button shows loading spinner
- ✅ Message appears in the chat immediately
- ✅ Message aligned to right (own message)
- ✅ Timestamp shows "Just now"
- ✅ Input clears after sending

**Verification:**
7. Close the sidebar (click X or backdrop)
8. Reopen chat for the same motard

**Expected Results:**
- ✅ Same conversation loads
- ✅ Your message is still there
- ✅ ConversationId was preserved

---

### 2️⃣ Re-open Same Chat

**Steps:**
1. With an existing conversation, close the sidebar
2. Reopen chat for the same motard and tour

**Expected Results:**
- ✅ Conversation loads from cache (fast)
- ✅ All previous messages visible
- ✅ Scroll position at bottom
- ✅ Same conversationId maintained

**Verification:**
3. Send another message
4. Close and reopen again

**Expected Results:**
- ✅ Both messages visible
- ✅ No duplicate messages
- ✅ Order preserved (oldest → newest)

---

### 3️⃣ Chat with Second Motard

**Steps:**
1. Navigate to a different motard profile
2. Click "Start Chat"
3. Observe empty conversation (new chat)
4. Send a message to this second motard

**Expected Results:**
- ✅ Independent conversation
- ✅ No messages from first motard visible
- ✅ Separate conversationId
- ✅ Message sends successfully

**Verification:**
5. Go back to first motard profile
6. Open chat again

**Expected Results:**
- ✅ First motard's messages still there
- ✅ Second motard's messages NOT visible
- ✅ Conversations are completely isolated

---

### 4️⃣ Multiple Tours - Same Motard TODO: need to create another tour

**Steps:**
1. Find a motard with multiple upcoming tours
2. Start chat (opens with first tour by default)
3. Send message: "Interested in [Tour A]"
4. Close sidebar

**Expected Results:**
- ✅ Conversation tied to specific tour
- ✅ Message sent successfully

**Future Test (when tour selection is added):**
5. If able to select different tour for same motard
6. Verify separate conversation is created

---

### 5️⃣ Page Reload / Browser Refresh

**Steps:**
1. Open a motard profile with existing conversation
2. Start chat and verify messages load
3. Refresh the entire page (F5 or Ctrl+R)
4. Wait for page to reload
5. Open chat again

**Expected Results:**
- ✅ Conversation list fetched from backend
- ✅ Existing conversation found by (tourId, motardUserId)
- ✅ All messages load correctly
- ✅ No data loss

---

### 6️⃣ Error Scenarios

#### 6a. Network Error on Load

**Steps:**
1. Open Developer Tools → Network tab
2. Throttle to "Offline" mode
3. Try to open chat

**Expected Results:**
- ✅ Loading state shows briefly
- ✅ Error message appears: "Failed to load conversation"
- ✅ Retry button visible
- ✅ No crash or blank screen

**Action:**
4. Switch back to "Online"
5. Click Retry

**Expected Results:**
- ✅ Conversation loads successfully

#### 6b. Send Message Failure

**Steps:**
1. With chat open, go offline
2. Type message: "This should fail"
3. Click Send

**Expected Results:**
- ✅ Send button shows spinner briefly
- ✅ Error toast/banner appears: "Failed to send message"
- ✅ Message still in input (draft preserved)
- ✅ User can edit and retry

**Action:**
4. Go back online
5. Click Send again

**Expected Results:**
- ✅ Message sends successfully
- ✅ No duplicate attempts

---

### 7️⃣ Mobile / Responsive Behavior

**Steps:**
1. Narrow browser to mobile width (< 768px)
2. Open motard profile
3. Click floating chat button (FAB bottom-right)

**Expected Results:**
- ✅ Sidebar is full-screen
- ✅ Close button visible (top-right, white circle with X)
- ✅ Close button has higher z-index (visible above all content)
- ✅ Message input not covered by keyboard (TODO:test on real device)
- ✅ Scrolling works smoothly

---

### 8️⃣ Auto-scroll Behavior

**Steps:**
1. Open existing conversation with 5+ messages
2. Observe scroll position

**Expected Results:**
- ✅ Automatically scrolled to bottom (latest message)
- ✅ Smooth scroll animation (CSS scroll-behavior: smooth)

**Action:**
3. Send a new message

**Expected Results:**
- ✅ Auto-scrolls to show new message
- ✅ Smooth scroll animation
- ✅ New message visible at bottom

**Additional Test:**
4. Manually scroll up to older messages
5. Send another new message

**Expected Results:**
- ✅ Auto-scrolls back to bottom
- ✅ New message appears and is visible

---

### 9️⃣ Request Booking Button

**Steps:**
1. Open chat with an existing conversation
2. Locate "Request Booking" button in header

**Expected Results:**
- ✅ Button disabled if no conversationId
- ✅ Button enabled after first message sent

**Action:**
3. Click "Request Booking"

**Expected Results:**
- ✅ Console logs: tourId and motardUserId
- ✅ (Future: navigates to booking request page)

---

### 🔟 Character Limit & Validation

**Steps:**
1. Open chat
2. Type 999 characters in input
3. Observe character counter appears

**Expected Results:**
- ✅ Counter shows "999 / 1000"
- ✅ Send button enabled

**Action:**
4. Add 2 more characters (1001 total)

**Expected Results:**
- ✅ Input truncates or prevents typing
- ✅ Counter shows "1000 / 1000"

**Action:**
5. Try sending empty message (spaces only)

**Expected Results:**
- ✅ Send button disabled
- ✅ Nothing happens on click

---

## Performance Checks

### Cache Effectiveness
- [ ] **First load**: Fetches from API
- [ ] **Subsequent opens**: Loads from cache (instant)
- [ ] **After send**: Message appears immediately (no refetch)

### Memory Management
- [ ] **Close sidebar**: Subscriptions cleaned up (no leaks)
- [ ] **Navigate away**: destroy$ completes properly
- [ ] **Multiple motards**: Each conversation cached separately

---

## UX Polish Checks

- [ ] **Loading states**: Spinner shows during API calls
- [ ] **Error states**: Clear error messages with retry options
- [ ] **Empty states**: Friendly "Start the conversation!" message
- [ ] **Timestamps**: Relative time ("2m ago", "1h ago", etc.)
- [ ] **Message bubbles**: Clear visual distinction (own vs. other)
- [ ] **Animations**: Smooth slide-in/out transitions
- [ ] **Backdrop click**: Closes sidebar (doesn't block)

---

## Data Consistency Checks

### Before Polling
- [ ] **Refresh page**: Messages persist
- [ ] **Switch tabs**: Return to same state
- [ ] **Clear browser cache**: Fetches from backend correctly

### After Send
- [ ] **Message order**: Newest at bottom
- [ ] **No duplicates**: Each message appears once
- [ ] **Sender ID**: Correct userId on own messages

---

## Next Steps After Testing

If all checks pass:
1. ✅ **Add polling** (Step 6) - Auto-refresh messages every 3-5s
2. ✅ **Enhance context bar** (Step 8) - Add tour details
3. ✅ **End-to-end test** (Step 9) - Full flow with backend

If issues found:
- Document specific failures
- Fix critical bugs before adding polling
- Re-test affected scenarios

---

## Known Limitations (Stage F5 v1)

- ❌ No typing indicators yet
- ❌ No read receipts
- ❌ No real-time updates (polling comes next)
- ❌ No unread count badges
- ❌ No conversation list view
- ❌ No message search
- ❌ No image/file attachments

These are **intentional** for F5. They'll be added in Phase 2 or F6.
