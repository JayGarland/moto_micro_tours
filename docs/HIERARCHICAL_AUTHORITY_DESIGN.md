# Hierarchical Authority Model - Design Document

**Date:** January 18, 2026  
**Status:** Design Phase - Awaiting Approval  
**Replaces:** Option E (Direct Set by Motard)

---

## Executive Summary

Redesigning the booking start time approval system to implement a hierarchical authority model with explicit organizer role and request/approve workflow.

**Key Changes:**
1. **Explicit Organizer:** Tour creator becomes distinct authority figure
2. **Request/Approve Workflow:** Participants and motards can request; motard/organizer approves
3. **Policy-Based Authorization:** Approval authority determined by deviation from scheduled time

---

## Authority Hierarchy

```
Tour Level:
  Organizer (Tour Creator)
    ├─ Full control over tour and all bookings
    ├─ Can approve/reject any start time requests
    └─ Can override motard decisions

Booking Level:
  Booking's Motard
    ├─ Admin over their assigned bookings
    ├─ Can approve "reasonable" time changes (within policy)
    └─ Must escalate to organizer for major deviations

Participant
    ├─ Can request start time changes
    └─ No approval authority
```

---

## Task A: Explicit Organizer Concept

### 1.1 Database Schema Changes

#### Option A: Tour-Level Organizer (RECOMMENDED)

**Add to Tour Entity:**
```csharp
public class Tour
{
    // ... existing fields
    public string OrganizerUserId { get; set; } = null!; // NEW: Tour creator/admin
    
    // Navigation
    public AppUser? Organizer { get; set; } // NEW
}
```

**Migration Logic:**
```csharp
// For existing tours:
// Set OrganizerUserId = first TourMotard.MotardUserId (ordered by TourMotard.Id or RowVersion)
UPDATE Tours t
SET t.OrganizerUserId = (
    SELECT tm.MotardUserId 
    FROM TourMotards tm 
    WHERE tm.TourId = t.Id 
    ORDER BY tm.RowVersion ASC 
    LIMIT 1
)
```

**Pros:**
- ✅ Clear single source of truth
- ✅ Efficient queries (no join to TourMotards)
- ✅ Supports future scenarios (organizer delegates but remains admin)

**Cons:**
- ❌ Data duplication if organizer is also a TourMotard

#### Option B: TourMotard-Level Role Flag

**Add to TourMotard Entity:**
```csharp
public class TourMotard
{
    // ... existing fields
    public bool IsOrganizer { get; set; } // NEW: Default false
}
```

**Pros:**
- ✅ No additional fields on Tour
- ✅ Organizer is always a TourMotard

**Cons:**
- ❌ Requires JOIN for organizer queries
- ❌ Must enforce "exactly one IsOrganizer = true" per tour
- ❌ Harder to query across tours

### **RECOMMENDATION: Option A (Tour.OrganizerUserId)**

**Rationale:**
- Simpler queries
- Clearer authority model
- Easier to extend (e.g., transfer organizer role)

---

## Task B: Authorization Helpers & Policies

### 2.1 Authorization Helper Methods

**Location:** Create `Services/AuthorizationHelpers.cs`

```csharp
public interface IAuthorizationHelpers
{
    Task<bool> IsTourOrganizerAsync(string userId, Guid tourId);
    Task<bool> IsTourMotardMemberAsync(string userId, Guid tourId);
    Task<bool> IsBookingMotardAsync(string userId, Guid bookingId);
    Task<string?> GetTourOrganizerIdAsync(Guid tourId);
}

public class AuthorizationHelpers : IAuthorizationHelpers
{
    private readonly AppDbContext _context;
    
    public async Task<bool> IsTourOrganizerAsync(string userId, Guid tourId)
    {
        var tour = await _context.Tours
            .Where(t => t.Id == tourId)
            .Select(t => t.OrganizerUserId)
            .FirstOrDefaultAsync();
        
        return tour == userId;
    }
    
    public async Task<bool> IsTourMotardMemberAsync(string userId, Guid tourId)
    {
        return await _context.TourMotards
            .AnyAsync(tm => tm.TourId == tourId && tm.MotardUserId == userId);
    }
    
    public async Task<bool> IsBookingMotardAsync(string userId, Guid bookingId)
    {
        return await _context.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => b.TourMotard!.MotardUserId)
            .FirstOrDefaultAsync() == userId;
    }
    
    public async Task<string?> GetTourOrganizerIdAsync(Guid tourId)
    {
        return await _context.Tours
            .Where(t => t.Id == tourId)
            .Select(t => t.OrganizerUserId)
            .FirstOrDefaultAsync();
    }
}
```

### 2.2 Authorization Matrix

| Action | Participant | Booking's Motard | Other Motard | Organizer |
|--------|-------------|------------------|--------------|-----------|
| **Request Start Time** | ✅ | ✅ | ❌ | ✅ |
| **Approve (Within Policy)** | ❌ | ✅ | ❌ | ✅ |
| **Approve (Outside Policy)** | ❌ | ❌ | ❌ | ✅ Only |
| **Reject Request** | ❌ | ✅ | ❌ | ✅ |
| **View Request** | ✅ (own) | ✅ (own bookings) | ❌ | ✅ (all) |
| **Complete Booking** | ✅ (own) | ✅ (own bookings) | ❌ | ❌* |

*Organizer cannot directly complete bookings (maintains mutual handshake)

### 2.3 Code Wording Audit

**Files Requiring Updates:**

1. **BookingsController.cs** (Line ~1106)
   - OLD: `"attempted to approve start time for booking {BookingId} they don't organize"`
   - NEW: `"attempted to approve start time for booking {BookingId} they are not assigned to"`

2. **Option E Documentation**
   - Replace all "organizer (motard)" → "booking's motard"
   - Add new section explaining organizer vs motard distinction

3. **Comments/Logs**
   - Search: "organiz" → verify context
   - Update to: "tour organizer" or "booking motard" as appropriate

---

## Task C: Request/Approve Workflow Design

### 3.1 Database Schema

#### New Enum: StartTimeOverrideStatus

```csharp
public enum StartTimeOverrideStatus
{
    None = 0,           // No request made
    Pending = 1,        // Requested, awaiting approval
    Approved = 2,       // Approved by motard or organizer
    Rejected = 3,       // Rejected by motard or organizer
    Cancelled = 4       // Requester cancelled before approval
}
```

#### Updated Booking Entity

```csharp
public class Booking
{
    // ... existing fields
    
    // Start Time Override Workflow (replaces direct-set Option E)
    public DateTime? RequestedStartAtUtc { get; set; }           // NEW: Proposed time
    public string? RequestedStartReason { get; set; }            // NEW: Why requesting
    public DateTime? RequestedStartAtUtcTimestamp { get; set; }  // NEW: When requested
    public string? RequestedByUserId { get; set; }               // NEW: Who requested
    
    public DateTime? ApprovedStartAtUtc { get; set; }            // EXISTS: Effective time
    public string? ApprovedStartReason { get; set; }             // EXISTS: Why approved
    public DateTime? ApprovedStartByMotardAtUtc { get; set; }    // EXISTS: When approved
    public string? ApprovedByUserId { get; set; }                // NEW: Who approved (motard or organizer)
    
    public StartTimeOverrideStatus StartTimeOverrideStatus { get; set; } // NEW: Current state
    public string? RejectionReason { get; set; }                 // NEW: Why rejected
    public DateTime? RejectedAtUtc { get; set; }                 // NEW: When rejected
    
    // ... rest of fields
}
```

### 3.2 State Machine

```
Initial State: None
    ↓
[Participant/Motard] POST /request-start-time
    ↓
State: Pending
    ├─→ [Motard/Organizer] POST /approve-start-time → Approved
    ├─→ [Motard/Organizer] POST /reject-start-time → Rejected
    └─→ [Requester] DELETE /request-start-time → Cancelled

Terminal States: Approved, Rejected, Cancelled
    ↓
Can request again (resets to Pending)
```

**State Transition Rules:**

| From State | Action | To State | Who Can Perform |
|------------|--------|----------|-----------------|
| None | Request | Pending | Participant, Booking Motard |
| Pending | Approve (in policy) | Approved | Booking Motard, Organizer |
| Pending | Approve (out of policy) | Approved | Organizer Only |
| Pending | Reject | Rejected | Booking Motard, Organizer |
| Pending | Cancel | Cancelled | Requester Only |
| Rejected | Request | Pending | Participant, Booking Motard |
| Cancelled | Request | Pending | Participant, Booking Motard |
| Approved | Request | Pending* | Requires new request (overrides) |

*Requesting after approval: Resets state, old approved time cleared

### 3.3 Approval Policy

**Default Policy (Configurable):**

```csharp
public class StartTimeApprovalPolicy
{
    // Motard can approve if deviation is within:
    public static readonly TimeSpan MotardApprovalWindow = TimeSpan.FromHours(24);
    
    // Any change before Tour.StartAt requires organizer
    public static readonly bool RequireOrganizerForEarlyStart = true;
}
```

**Authorization Logic:**

```csharp
public bool RequiresOrganizerApproval(DateTime requestedTime, DateTime tourStartAt)
{
    // Early start always requires organizer
    if (requestedTime < tourStartAt && RequireOrganizerForEarlyStart)
        return true;
    
    // Calculate absolute deviation
    var deviation = Math.Abs((requestedTime - tourStartAt).TotalHours);
    
    // Within 24h window: motard can approve
    // Outside window: organizer required
    return deviation > MotardApprovalWindow.TotalHours;
}
```

**Examples:**

| Tour StartAt | Requested Time | Deviation | Can Motard Approve? |
|--------------|----------------|-----------|---------------------|
| 2026-01-20 18:00 | 2026-01-20 15:00 | -3h (early) | ❌ Organizer Required |
| 2026-01-20 18:00 | 2026-01-20 20:00 | +2h | ✅ Within Policy |
| 2026-01-20 18:00 | 2026-01-21 19:00 | +25h | ❌ Organizer Required |
| 2026-01-20 18:00 | 2026-01-19 18:00 | -24h (early) | ❌ Organizer Required |

### 3.4 Endpoint Design

#### 3.4.1 Request Start Time

**Endpoint:** `POST /api/bookings/{id}/request-start-time`

**Authorization:**
- Participant of the booking
- Booking's motard
- Tour organizer (optional)

**Request Body:**
```json
{
  "requestedStartAtUtc": "2026-01-20T15:00:00Z",
  "reason": "Tour finished early, participants ready to complete"
}
```

**Validation:**
- Booking must be Paid
- Requested time cannot be > 1 year in future
- Requested time must be different from current Tour.StartAt or ApprovedStartAtUtc

**Response:**
```json
{
  "bookingId": "...",
  "startTimeOverrideStatus": "Pending",
  "requestedStartAtUtc": "2026-01-20T15:00:00Z",
  "requiresOrganizerApproval": true,
  "reason": "Requested time is before tour start - organizer approval required"
}
```

**State Changes:**
- `StartTimeOverrideStatus` → `Pending`
- `RequestedStartAtUtc` = request.RequestedStartAtUtc
- `RequestedStartReason` = request.Reason
- `RequestedStartAtUtcTimestamp` = DateTime.UtcNow
- `RequestedByUserId` = currentUser.Id

**Error Cases:**
- 400: Booking not Paid
- 400: Already has pending request (must cancel first)
- 403: User not participant/motard/organizer
- 404: Booking not found

#### 3.4.2 Approve Start Time

**Endpoint:** `POST /api/bookings/{id}/approve-start-time`

**Authorization:**
- Booking's motard (if within policy)
- Tour organizer (always)

**Request Body:**
```json
{
  "approvalReason": "Approved for early completion"
}
```

**Validation:**
- Booking must have `StartTimeOverrideStatus = Pending`
- Must have `RequestedStartAtUtc` set
- If requires organizer approval: user must be organizer

**Response:**
```json
{
  "bookingId": "...",
  "startTimeOverrideStatus": "Approved",
  "approvedStartAtUtc": "2026-01-20T15:00:00Z",
  "effectiveStartAtUtc": "2026-01-20T15:00:00Z",
  "approvedByUserId": "...",
  "approvedByRole": "Organizer"
}
```

**State Changes:**
- `StartTimeOverrideStatus` → `Approved`
- `ApprovedStartAtUtc` = `RequestedStartAtUtc`
- `ApprovedStartReason` = request.ApprovalReason OR RequestedStartReason
- `ApprovedStartByMotardAtUtc` = DateTime.UtcNow (rename to ApprovedAtUtc)
- `ApprovedByUserId` = currentUser.Id

**Error Cases:**
- 400: No pending request
- 403: User not authorized (not motard/organizer, or organizer required but user is motard)
- 404: Booking not found

#### 3.4.3 Reject Start Time

**Endpoint:** `POST /api/bookings/{id}/reject-start-time`

**Authorization:**
- Booking's motard
- Tour organizer

**Request Body:**
```json
{
  "rejectionReason": "Requested time conflicts with other bookings"
}
```

**Response:**
```json
{
  "bookingId": "...",
  "startTimeOverrideStatus": "Rejected",
  "rejectionReason": "Requested time conflicts with other bookings",
  "requestedStartAtUtc": "2026-01-20T15:00:00Z"
}
```

**State Changes:**
- `StartTimeOverrideStatus` → `Rejected`
- `RejectionReason` = request.RejectionReason
- `RejectedAtUtc` = DateTime.UtcNow

**Note:** `RequestedStartAtUtc` preserved for history

#### 3.4.4 Cancel Request (Optional)

**Endpoint:** `DELETE /api/bookings/{id}/request-start-time`

**Authorization:**
- Original requester only

**Response:** 204 No Content

**State Changes:**
- `StartTimeOverrideStatus` → `Cancelled`
- Preserve all request fields for audit

### 3.5 Frontend Integration Points

**New UI Components:**

1. **Request Start Time Dialog** (Participant & Motard)
   - Date/time picker
   - Reason textarea
   - Shows policy warning if organizer approval required

2. **Pending Request Badge** (Booking Detail)
   - Shows "Start Time Change Requested"
   - Displays requested time and reason
   - Shows requester name

3. **Approval Actions** (Motard & Organizer)
   - "Approve" button (disabled if organizer required and user is motard)
   - "Reject" button with reason dialog
   - Shows who can approve

4. **Request History Timeline** (Optional)
   - Shows all requests, approvals, rejections
   - Audit trail

---

## Task D: Completion Gate (Unchanged)

**EffectiveStartAtUtc Calculation:**
```csharp
var effectiveStartAtUtc = booking.ApprovedStartAtUtc ?? tour.StartAt;
```

**Completion Rules:**
- Both participant and motard must wait until `now >= effectiveStartAtUtc`
- Mutual handshake preserved
- Reviews still gated by `CompletedAtUtc != null`

**No changes needed** - existing Option E completion logic works as-is.

---

## Migration Strategy

### Phase 1: Add Organizer Field
```sql
ALTER TABLE Tours ADD OrganizerUserId VARCHAR(450) NULL;
ALTER TABLE Tours ADD CONSTRAINT FK_Tours_OrganizerUserId 
    FOREIGN KEY (OrganizerUserId) REFERENCES AspNetUsers(Id);

-- Set organizer = first motard
UPDATE Tours t
SET t.OrganizerUserId = (
    SELECT tm.MotardUserId 
    FROM TourMotards tm 
    WHERE tm.TourId = t.Id 
    ORDER BY tm.RowVersion ASC 
    LIMIT 1
);

-- Make non-nullable
ALTER TABLE Tours MODIFY OrganizerUserId VARCHAR(450) NOT NULL;
```

### Phase 2: Add Request/Approve Fields
```sql
ALTER TABLE Bookings ADD RequestedStartAtUtc DATETIME(6) NULL;
ALTER TABLE Bookings ADD RequestedStartReason LONGTEXT NULL;
ALTER TABLE Bookings ADD RequestedStartAtUtcTimestamp DATETIME(6) NULL;
ALTER TABLE Bookings ADD RequestedByUserId VARCHAR(450) NULL;
ALTER TABLE Bookings ADD ApprovedByUserId VARCHAR(450) NULL;
ALTER TABLE Bookings ADD StartTimeOverrideStatus INT NOT NULL DEFAULT 0;
ALTER TABLE Bookings ADD RejectionReason LONGTEXT NULL;
ALTER TABLE Bookings ADD RejectedAtUtc DATETIME(6) NULL;

-- Add FK constraints
ALTER TABLE Bookings ADD CONSTRAINT FK_Bookings_RequestedByUserId 
    FOREIGN KEY (RequestedByUserId) REFERENCES AspNetUsers(Id);
ALTER TABLE Bookings ADD CONSTRAINT FK_Bookings_ApprovedByUserId 
    FOREIGN KEY (ApprovedByUserId) REFERENCES AspNetUsers(Id);
```

### Phase 3: Migrate Existing Approved Times
```sql
-- For bookings that already have ApprovedStartAtUtc from Option E:
-- Mark as Approved with organizer as approver
UPDATE Bookings b
INNER JOIN TourMotards tm ON b.TourMotardId = tm.Id
INNER JOIN Tours t ON tm.TourId = t.Id
SET 
    b.StartTimeOverrideStatus = 2, -- Approved
    b.ApprovedByUserId = t.OrganizerUserId,
    b.ApprovedStartReason = COALESCE(b.ApprovedStartReason, 'Migrated from Option E')
WHERE b.ApprovedStartAtUtc IS NOT NULL;
```

---

## Risk Assessment

### High Risk
- ⚠️ **Breaking Change:** Existing Option E implementations must be updated
- ⚠️ **Data Migration:** Setting organizer incorrectly breaks authorization
- ⚠️ **State Machine Bugs:** Complex state transitions need thorough testing

### Medium Risk
- ⚠️ **Performance:** Additional JOINs for organizer checks
- ⚠️ **Policy Confusion:** Users may not understand why organizer approval needed

### Low Risk
- ⚠️ **UI Complexity:** More buttons and states to handle

---

## Testing Checklist

### Unit Tests
- [ ] IsTourOrganizerAsync returns true for organizer
- [ ] IsTourOrganizerAsync returns false for non-organizer motard
- [ ] RequiresOrganizerApproval policy logic (all edge cases)
- [ ] State machine transitions (valid and invalid)

### Integration Tests
- [ ] Request → Approve → Complete flow
- [ ] Request → Reject → Re-request flow
- [ ] Motard attempts organizer-required approval (403)
- [ ] Organizer approves out-of-policy request (200)
- [ ] Participant requests, motard approves in-policy (200)

### E2E Tests
- [ ] Full workflow: request, approve, complete, review
- [ ] Authorization matrix: test each role/action combination
- [ ] Migration: existing tours have correct organizer
- [ ] Migration: existing Option E approvals preserved

---

## Open Questions

1. **Can organizer transfer role?** (Not in scope for now)
2. **Can organizer remove themselves as motard?** (Block if they're the only motard)
3. **Notification system?** (Email/push when request pending, approved, rejected)
4. **Audit log?** (Track all state changes for disputes)
5. **Multiple simultaneous requests?** (Block - only one pending at a time)

---

## Approval Required

**This document requires approval before implementation begins.**

**Estimated Implementation Time:**
- Task A (Organizer): 2-3 hours
- Task B (Authorization): 2-3 hours
- Task C (Request/Approve): 6-8 hours
- Task D (No changes): 0 hours
- Testing: 4-6 hours
- **Total: 14-20 hours**

**Review Requested From:**
- Product Owner (policy decisions)
- Senior Developer (architecture review)
- QA Lead (testing strategy)

---

**Next Step:** Await approval, then begin implementation with Task A.
