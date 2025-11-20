# Pre-booking Inbox - Testing Guide

## Two-Browser Polling Test Setup

### Goal
Test real-time message delivery using polling (3-second interval) between two users.

---

## Setup

### Browser A: Participant User
1. Open browser (or incognito window)
2. Navigate to `http://localhost:4200`
3. Login as a **Participant** user
4. Navigate to `/messages` (or click "Messages" in header)

### Browser B: Motard User (or Second Participant)
1. Open different browser (or incognito window)
2. Navigate to `http://localhost:4200`
3. Login as a **Motard** user (or different Participant)
4. Navigate to `/messages`

---

## Test Scenarios

### Test 1: Start Conversation from Profile
**Browser A (Participant):**
1. Navigate to a motard's profile page
2. Click "Chat before booking" button
3. Send first message: "Hi, interested in your tour"
4. Leave chat open

**Expected:**
- ✅ Message appears immediately in chat
- ✅ Polling starts (every 3 seconds)

**Browser B (Motard):**
1. Navigate to `/messages`
2. Wait up to 3 seconds

**Expected:**
- ✅ New conversation appears in list
- ✅ Preview shows: "Hi, interested in your tour"
- ✅ Click conversation → Opens chat with message

---

### Test 2: Bi-directional Polling
**Browser A (Participant) - Chat Open:**
- Keep chat open from Test 1

**Browser B (Motard):**
1. Open same conversation from inbox
2. Send message: "Hello! Happy to answer questions"

**Expected:**
- ✅ Message appears in Browser B immediately
- ✅ Within 3 seconds, message appears in Browser A (polling!)
- ✅ Auto-scroll to new message in Browser A

**Browser A:**
1. Send reply: "What's the difficulty level?"

**Expected:**
- ✅ Message appears in Browser A immediately
- ✅ Within 3 seconds, message appears in Browser B
- ✅ Auto-scroll in Browser B

---

### Test 3: Multiple Conversations
**Browser A (Participant):**
1. Navigate to `/messages`
2. Should see conversation from Test 1/2

**Browser B (Motard):**
1. Navigate to a different participant's profile
2. Start new conversation: "Check out my new tour!"

**Browser A (Different Participant):**
1. Navigate to `/messages`
2. Wait up to 3 seconds

**Expected:**
- ✅ New conversation appears
- ✅ Conversations sorted by newest first

---

### Test 4: Inbox List Updates
**Browser A:**
1. In inbox, DON'T open any conversation
2. Have another user send message to you

**Expected:**
- ✅ Conversation list does NOT auto-update (no polling on list)
- ✅ Click refresh button (🔄) → Conversation moves to top
- ✅ Last message preview updates

> **Note:** Currently only active chats have polling, not the inbox list.
> TODO:Enhancement: Add polling to inbox list in Phase 2.

---

### Test 5: Polling Lifecycle
**Browser A:**
1. Open conversation from inbox
2. Open DevTools → Network tab
3. Filter: `messages`

**Expected:**
- ✅ Initial GET request for messages
- ✅ Every 3 seconds: GET request to same endpoint
- ✅ No requests when chat is closed

**Test Cleanup:**
1. Close chat sidebar

**Expected:**
- ✅ Polling stops (no more network requests)
- ✅ Conversation remains in inbox list

---

### Test 6: Mobile Responsive
**Browser A (narrow to < 768px):**
1. Navigate to `/messages`

**Expected:**
- ✅ Conversation list takes full width
- ✅ Click conversation → Chat takes full screen
- ✅ List is hidden when chat opens
- ✅ Close button returns to list

---

## Key Features to Verify

### Inbox Page (`/messages`)
- [x] Shows all conversations for current user
- [x] Sorted by newest (last message time)
- [x] Each item shows:
  - Other user's name
  - Tour title
  - Last message preview (truncated)
  - Relative time ("2h ago", "Yesterday")
- [x] Avatar with first letter of name
- [x] Click conversation → Opens chat on right (desktop) or full-screen (mobile)
- [x] Selected conversation highlighted
- [x] Empty state when no conversations
- [x] Refresh button to reload list

### Polling Behavior
- [x] Polling starts when chat opens
- [x] Polling interval: 3 seconds
- [x] Only polls active conversation
- [x] Polling stops when chat closes
- [x] New messages appear automatically (within 3 sec)
- [x] Auto-scroll to new messages
- [x] No duplicate messages

### Chat Integration
- [x] Chat-sidebar works in two modes:
  - Mode 1: From motard profile (find/create conversation)
  - Mode 2: From inbox (direct conversationId)
- [x] All existing chat features work:
  - Send/receive messages
  - Auto-scroll
  - Loading states
  - Error handling
  - Character limit (1000)

---

## Common Issues & Fixes

### Issue: "Messages not appearing in other browser"
**Check:**
- Is polling active? (Network tab should show requests every 3 sec)
- Are both users in the same conversation?
- Is backend running?
- Are cookies enabled (auth required)?

### Issue: "Conversation not showing in inbox"
**Fix:**
- Click refresh button (🔄) in inbox
- Backend creates conversation on first message
- Check browser console for errors

### Issue: "Auto-scroll not working"
**Check:**
- Are new messages arriving? (Check DevTools → Network)
- Is polling active?
- Try scrolling manually then let new message arrive

---

## Next Steps After Testing

1. **Add Context Bar (Step 9):**
   - Show tour details in chat header
   - Display motard rating, price range

2. **End-to-End Testing (Step 10):**
   - Full flow: Profile → Chat → Book
   - Authorization testing
   - Error scenarios

3. **Phase 2 Enhancements:**
   - SignalR for true real-time (no polling delay)
   - Typing indicators
   - Unread message badges
   - Poll inbox list for new conversations
   - Online/offline status

---

## Success Criteria

✅ Two users can chat in real-time (3-second delay max)  
✅ Messages persist across page refreshes  
✅ Inbox shows all conversations for current user  
✅ Polling starts/stops correctly (no memory leaks)  
✅ Works for both Participant and Motard roles  
✅ Mobile responsive (list + chat on separate screens)  
✅ No duplicate messages or lost messages  
✅ Auto-scroll keeps latest message visible
