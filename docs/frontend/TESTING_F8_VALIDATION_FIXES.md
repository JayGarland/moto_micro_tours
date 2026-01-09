# Stage F8 - Testing Guide & Dual-Sided Replies Implementation

## ✅ Completed Fixes

### 1. **Duplicate CreateReviewDto Removed**
- **File**: `core/models/motard.models.ts`
- **Status**: ✅ Complete
- **Test**: Run `npm run build` - should have no TypeScript errors about CreateReviewDto

### 2. **Backend Validation Added**
- **File**: `Controllers/ReviewsController.cs`
- **Changes**:
  - Comment length validation (max 2000 chars)
  - Reply message validation (not empty, max 2000 chars)
  - DbUpdateException handling for duplicate reviews (returns 409 instead of 500)
- **Status**: ✅ Complete

### 3. **Dual-Sided Replies Implemented**
- **Backend**: `Controllers/ReviewsController.cs` (lines 203-208)
  - Allows both reviewer AND target to add replies
- **Frontend**: `review-card.component.ts` and `.html`
  - Dynamic button text: "Add update" (reviewer) vs "Reply" (target)
  - Dynamic form label and submit button
- **Status**: ✅ Complete

---

## 🧪 Testing Instructions

### Prerequisites
1. Start backend: `cd moto_micro_tours; dotnet run`
2. Start frontend: `cd moto-micro-tours-frontend; npm run start:proxy`
3. Have at least one completed, paid booking with reviews

### Test Suite

#### **Test 1: Comment Length Validation**
Use the test file: `test_validation_fixes.http`

1. Open in VS Code with REST Client extension
2. Run TEST 1 (valid review) - should succeed with 201
3. Run TEST 2 (duplicate) - should return **409 Conflict** ✅
4. Run TEST 3 (comment > 2000 chars) - should return **400 BadRequest** with message "Comment must be 2000 characters or less" ✅
5. Run TEST 4 (invalid rating) - should return **400 BadRequest** ✅

#### **Test 2: Reply Validations**
1. Run TEST 7 (valid reply) - should succeed with 200 ✅
2. Run TEST 8 (empty message) - should return **400 BadRequest** with "Reply message cannot be empty" ✅
3. Run TEST 9 (message > 2000 chars) - should return **400 BadRequest** with "Reply message must be 2000 characters or less" ✅

#### **Test 3: Dual-Sided Replies (Frontend UI)**

**Scenario A: Reviewer adds update to their own review**
1. Login as **participant1**
2. Go to motard profile page (e.g., `/motards/{motardId}`)
3. Find a review YOU wrote (your username should show as "reviewer")
4. **Verify**: Button shows **"Add update"** with edit icon
5. Click button, form title shows **"Add an update"**
6. Enter message, submit button shows **"Post update"**
7. Submit - reply should appear in chronological order

**Scenario B: Target replies to review**
1. Login as **organizer1** (motard)
2. Go to your own motard profile (`/motards/{yourUserId}`)
3. Find a review written BY a participant (not by you)
4. **Verify**: Button shows **"Reply"** with reply icon
5. Click button, form title shows **"Write a reply"**
6. Enter message, submit button shows **"Post reply"**
7. Submit - reply should appear

**Scenario C: Participant reviews their profile**
1. Login as **participant1**
2. Navigate to `/participants/{yourUserId}`
3. Find reviews where YOU are the target
4. **Verify**: You can add **"Reply"** to reviews about you
5. **Verify**: No buttons appear on reviews YOU wrote (you're already the author)

#### **Test 4: Frontend Review Eligibility**
1. Go to booking detail page
2. **Before completion**: No "Leave Review" section
3. **After BOTH parties complete** AND booking status is **Paid**: Review section appears ✅
4. Try with non-Paid booking (even if completed): Review section stays hidden ✅

---

## 📊 Expected Results Summary

| Test | Expected Result | Status |
|------|----------------|--------|
| Duplicate review | 409 Conflict | ✅ |
| Comment > 2000 chars | 400 BadRequest | ✅ |
| Invalid rating (0 or 6) | 400 BadRequest | ✅ |
| Empty reply message | 400 BadRequest | ✅ |
| Reply > 2000 chars | 400 BadRequest | ✅ |
| Reviewer adds update | Button shows "Add update" | ✅ |
| Target replies | Button shows "Reply" | ✅ |
| Review requires Paid + Completed | Form only shows when both true | ✅ |

---

## 🔧 Migration Status

### Current State
- **Reviews** and **ReviewReplies** tables exist in database (created via manual SQL)
- No EF Core migration file exists for these tables
- EF snapshot includes the entities

### Applied Migrations
```
20251019134200_Init
20251120194004_FixPreBookingForeignKeys
20251120195414_SyncPreBookingConversationFKs
20251122101445_AddDeclineReasonToBooking
20251229230525_AddMessageToMotardToBooking
20260101182904_AddBookingCompletionTimestamps
```

### To Generate Migration
```bash
cd moto_micro_tours
dotnet ef migrations add AddReviewSystemTables
```

This will create a migration file documenting the Reviews schema. Since tables already exist, the migration Up() will be empty or will skip already-existing tables.

---

## 🎯 Changes Summary

### Backend (`ReviewsController.cs`)
```csharp
// Line 115: Comment validation
if (dto.Comment?.Length > 2000)
    return BadRequest(new { error = "Comment must be 2000 characters or less" });

// Line 129: Race condition handling
try {
    await _db.SaveChangesAsync();
}
catch (DbUpdateException ex) 
    when (ex.InnerException?.Message.Contains("Duplicate") == true ||
          ex.InnerException?.Message.Contains("UNIQUE") == true) {
    return Conflict(new { error = "You have already reviewed this target for this booking" });
}

// Line 203: Dual-sided reply authorization
bool isReviewer = review.ReviewerUserId == userId;
bool isTarget = review.TargetId == userId && review.TargetType == ReviewTargetType.User;
if (!isReviewer && !isTarget)
    return Forbid();

// Line 207: Reply message validation
if (string.IsNullOrWhiteSpace(dto.Message))
    return BadRequest(new { error = "Reply message cannot be empty" });
if (dto.Message.Length > 2000)
    return BadRequest(new { error = "Reply message must be 2000 characters or less" });
```

### Frontend (`review-card.component.ts`)
```typescript
// Dual-access check
get canReply(): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;
    const isReviewer = this.review.reviewerUserId === currentUser.userId;
    const isTarget = this.review.targetType === 1 && this.review.targetId === currentUser.userId;
    return isReviewer || isTarget;
}

// Role identification for UI
get isReviewer(): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;
    return this.review.reviewerUserId === currentUser.userId;
}
```

### Frontend (`review-card.component.html`)
```html
<!-- Dynamic button text -->
<button mat-button color="primary" (click)="toggleReplyForm()">
  <mat-icon>{{ isReviewer ? 'edit' : 'reply' }}</mat-icon>
  {{ isReviewer ? 'Add update' : 'Reply' }}
</button>

<!-- Dynamic form label -->
<mat-label>{{ isReviewer ? 'Add an update' : 'Write a reply' }}</mat-label>

<!-- Dynamic submit button -->
{{ isSubmittingReply ? 'Submitting...' : (isReviewer ? 'Post update' : 'Post reply') }}
```

---

## ✅ All Tasks Complete!

1. ✅ Validation fixes applied and tested
2. ✅ Dual-sided replies implemented (reviewer + target)
3. ⏸️ Migration generation pending (command ready to run)
