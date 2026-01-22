# Phase 1: Manual Testing Script

**Prerequisites**: 
- Backend running on `https://localhost:7173`
- Frontend running on `http://localhost:4200`
- Database seeded with test data
- At least 3 test users available

---

## Test Setup

### Test Users
From `DbSeeder.cs`, we have:
1. **Alice** (MotardA) - `alice@test.com` / `Test123!`
2. **Bob** (MotardB) - `bob@test.com` / `Test123!`
3. **Charlie** (Participant) - `charlie@test.com` / `Test123!`

### Test Scenario
- **Tour**: "Loire Valley Discovery" (starts 30 days from now)
- **Organizer**: Alice (set via Phase 1 migration backfill)
- **Motards**: Alice (lead), Bob (support)
- **Booking**: Charlie books with Bob

---

## Test Case 1: Verify Tour Organizer is Set

**Objective**: Confirm Phase 1 migration correctly set `OrganizerUserId`

### Steps:
1. Login as Alice
2. Navigate to Tours → Tour Detail (Loire Valley Discovery)
3. Inspect network response from `GET /api/tours/{tourId}`

### Expected Result:
```json
{
  "id": "...",
  "title": "Loire Valley Discovery",
  "organizerUserId": "<Alice's user ID>", // ✅ Should be populated
  "startAt": "...",
  "motards": [
    {
      "tourMotardId": "...",
      "motardUserId": "<Alice's user ID>",
      "motardName": "Alice Smith"
    },
    {
      "tourMotardId": "...",
      "motardUserId": "<Bob's user ID>",
      "motardName": "Bob Johnson"
    }
  ]
}
```

**Pass Criteria**: 
- ✅ `organizerUserId` is not null
- ✅ `organizerUserId` matches Alice's user ID (first TourMotard by creation order)

---

## Test Case 2: Booking Motard Can Approve Start Time

**Objective**: Verify booking's motard can still approve effective start time

### Steps:
1. Login as Charlie
2. Create booking for Loire Valley tour with **Bob** as motard
3. Pay for booking → Status becomes `Paid`
4. Logout, login as **Bob**
5. Navigate to Bookings → Charlie's booking detail
6. Click "Approve Start Time" (or POST via API):
   ```http
   POST /api/bookings/{bookingId}/approve-start-time
   Authorization: Bearer <Bob's token>
   Content-Type: application/json

   {
     "approvedStartAtUtc": "2026-01-15T09:00:00Z",
     "reason": "Early arrival approved by booking motard"
   }
   ```

### Expected Result:
```json
{
  "bookingId": "...",
  "status": "Paid",
  "approvedStartAtUtc": "2026-01-15T09:00:00Z",
  "approvedStartReason": "Early arrival approved by booking motard",
  "approvedAtUtc": "2026-01-18T...", // ✅ Timestamp when approved
  "approvedByUserId": "<Bob's user ID>" // ✅ Bob's ID
}
```

**Pass Criteria**: 
- ✅ Request succeeds (200 OK)
- ✅ `approvedAtUtc` is set to current timestamp
- ✅ `approvedByUserId` matches Bob's user ID
- ✅ Response message: "Start time approved by booking motard"

---

## Test Case 3: Tour Organizer Can Approve Start Time

**Objective**: Verify tour organizer (Alice) can approve start time even if not the booking's motard

### Steps:
1. Use booking from Test Case 2 (Charlie → Bob)
2. Reset approval fields (manual DB update or create new booking):
   ```sql
   UPDATE Bookings 
   SET ApprovedStartAtUtc = NULL, 
       ApprovedAtUtc = NULL, 
       ApprovedByUserId = NULL
   WHERE Id = '<booking-id>';
   ```
3. Login as **Alice** (tour organizer, but NOT booking's motard)
4. Navigate to Bookings → Charlie's booking detail
5. POST approval request:
   ```http
   POST /api/bookings/{bookingId}/approve-start-time
   Authorization: Bearer <Alice's token>
   Content-Type: application/json

   {
     "approvedStartAtUtc": "2026-01-15T08:30:00Z",
     "reason": "Early start approved by tour organizer"
   }
   ```

### Expected Result:
```json
{
  "bookingId": "...",
  "approvedStartAtUtc": "2026-01-15T08:30:00Z",
  "approvedStartReason": "Early start approved by tour organizer",
  "approvedAtUtc": "2026-01-18T...", // ✅ Current timestamp
  "approvedByUserId": "<Alice's user ID>" // ✅ Alice's ID (NOT Bob's)
}
```

**Backend Log** (verify in console):
```
[INFO] User <Alice's ID> approved start time for booking <booking-id> 
       as tour organizer (2026-01-15 08:30:00 UTC). 
       Reason: Early start approved by tour organizer
```

**Pass Criteria**: 
- ✅ Request succeeds (200 OK)
- ✅ `approvedByUserId` matches Alice's user ID (the organizer, not Bob the motard)
- ✅ Backend log says "approved ... as tour organizer"

---

## Test Case 4: Non-Authorized User CANNOT Approve

**Objective**: Verify Charlie (participant) cannot approve his own booking's start time

### Steps:
1. Login as **Charlie** (participant, not motard or organizer)
2. Attempt to approve his own booking:
   ```http
   POST /api/bookings/{bookingId}/approve-start-time
   Authorization: Bearer <Charlie's token>
   Content-Type: application/json

   {
     "approvedStartAtUtc": "2026-01-15T10:00:00Z",
     "reason": "I want early start"
   }
   ```

### Expected Result:
```json
{
  "message": "User <Charlie's ID> attempted to approve start time for booking <booking-id>, 
              but is neither the booking's motard nor the tour organizer"
}
```

**Status Code**: 401 Unauthorized

**Pass Criteria**: 
- ✅ Request FAILS with 401 Unauthorized
- ✅ Error message clearly states "neither the booking's motard nor the tour organizer"
- ✅ Database fields remain unchanged (null)

---

## Test Case 5: Completion Handshake Uses Effective Start Time

**Objective**: Verify `EffectiveStartAtUtc` formula works correctly in completion endpoint

### Setup:
1. Use booking from Test Case 3 (has approved start time: `2026-01-15T08:30:00Z`)
2. Tour start time: `2026-02-18T09:00:00Z` (30 days from now)

### Steps:
1. Advance server time to after approved start time (or use test environment)
2. Login as Charlie → Mark booking complete (participant side)
3. Login as Bob → Mark booking complete (motard side)

### Expected Behavior:
Backend calculates:
```csharp
DateTime effectiveStartAt = booking.ApprovedStartAtUtc ?? booking.Tour.StartAt;
// = 2026-01-15T08:30:00Z (uses approved time, not tour start time)
```

**Completion Allowed If**: `utcNow >= effectiveStartAt`  
**Completion Blocked If**: `utcNow < effectiveStartAt`

### Expected Result:
```json
{
  "bookingId": "...",
  "status": "Completed",
  "completedByParticipantAtUtc": "2026-01-15T10:00:00Z",
  "completedByMotardAtUtc": "2026-01-15T10:05:00Z",
  "completedAtUtc": "2026-01-15T10:05:00Z" // ✅ Earlier than tour.StartAt
}
```

**Pass Criteria**: 
- ✅ Completion succeeds if current time >= approved start time
- ✅ Completion uses `ApprovedStartAtUtc` (not `Tour.StartAt`)
- ✅ Booking can be completed 30+ days before tour official start

---

## Test Case 6: Completion Without Approval Uses Tour.StartAt

**Objective**: Verify fallback to `Tour.StartAt` when no approval exists

### Setup:
1. Create new booking (Charlie → Bob) with NO approved start time
2. `ApprovedStartAtUtc = NULL`

### Steps:
1. Attempt completion before `Tour.StartAt`

### Expected Behavior:
```csharp
DateTime effectiveStartAt = booking.ApprovedStartAtUtc ?? booking.Tour.StartAt;
// = 2026-02-18T09:00:00Z (uses tour start time, no approval)
```

### Expected Result:
```json
{
  "message": "Tour has not started yet. Cannot complete booking before 2026-02-18 09:00:00 UTC."
}
```

**Status Code**: 400 Bad Request

**Pass Criteria**: 
- ✅ Completion blocked if before `Tour.StartAt`
- ✅ Error message shows tour start time (not null)

---

## Test Case 7: Database Audit Trail Verification

**Objective**: Confirm all audit fields are correctly populated

### Steps:
1. Use booking from Test Case 3 (approved by Alice as organizer)
2. Query database directly:
   ```sql
   SELECT 
     Id,
     ApprovedStartAtUtc,
     ApprovedStartReason,
     ApprovedAtUtc,
     ApprovedByUserId
   FROM Bookings
   WHERE Id = '<booking-id>';
   ```

### Expected Result:
```
Id: <booking-guid>
ApprovedStartAtUtc: 2026-01-15 08:30:00.000
ApprovedStartReason: Early start approved by tour organizer
ApprovedAtUtc: 2026-01-18 14:23:45.123 ✅ (timestamp when POST was made)
ApprovedByUserId: <Alice's user ID> ✅ (NOT Bob's ID)
```

**Pass Criteria**: 
- ✅ All 4 fields are NOT NULL
- ✅ `ApprovedAtUtc` is later than `ApprovedStartAtUtc`
- ✅ `ApprovedByUserId` matches organizer (Alice), not booking motard (Bob)

---

## Test Case 8: Multi-Motard Tour Organizer Authority

**Objective**: Verify organizer can approve bookings for ANY motard on the tour

### Setup:
1. Tour has 2 motards: Alice (organizer) + Bob (support)
2. Create 2 bookings:
   - Booking A: Charlie → **Alice** (organizer is also booking motard)
   - Booking B: Dave → **Bob** (organizer is NOT booking motard)

### Steps:
1. Login as **Alice**
2. Approve BOTH bookings:
   - POST `/api/bookings/<bookingA>/approve-start-time`
   - POST `/api/bookings/<bookingB>/approve-start-time`

### Expected Result:
**Booking A**: 
- ✅ Succeeds (Alice is booking motard)
- Log: "approved ... as booking motard"

**Booking B**: 
- ✅ Succeeds (Alice is tour organizer)
- Log: "approved ... as tour organizer"

**Pass Criteria**: 
- ✅ Both requests succeed
- ✅ Booking A log shows "booking motard" role
- ✅ Booking B log shows "tour organizer" role
- ✅ Both have `ApprovedByUserId = Alice's ID`

---

## Test Case 9: Frontend Display Verification

**Objective**: Confirm frontend correctly displays new fields

### Steps:
1. Login as Bob
2. Navigate to Bookings → Charlie's booking (approved by Alice)
3. Inspect booking detail card

### Expected UI Display:
```
Booking Details
------------------
Status: Paid
Effective Start Time: Jan 15, 2026 8:30 AM UTC ✅
Approved By: Alice Smith (Tour Organizer) ✅
Approved At: Jan 18, 2026 2:23 PM UTC ✅
Reason: Early start approved by tour organizer ✅

Original Tour Start: Feb 18, 2026 9:00 AM UTC
```

**Pass Criteria**: 
- ✅ UI shows `approvedStartAtUtc` field
- ✅ UI shows `approvedByUserId` resolved to user name
- ✅ UI distinguishes "Tour Organizer" vs "Booking Motard" role
- ✅ UI shows `approvedAtUtc` timestamp

---

## Test Case 10: Migration Rollback Test (Optional)

**Objective**: Verify migrations can be safely rolled back

### Steps:
1. Backup current database
2. Run migration rollback:
   ```bash
   dotnet ef database update AddReviewsTable
   ```
   (This rolls back Phase 1 migrations)

### Expected Schema After Rollback:
- ✅ `Tours.OrganizerUserId` column REMOVED
- ✅ `Tours.OrganizerId` FK REMOVED
- ✅ `Bookings.ApprovedAtUtc` RENAMED BACK to `ApprovedStartByMotardAtUtc`
- ✅ `Bookings.ApprovedByUserId` column REMOVED

### Steps to Re-Apply:
```bash
dotnet ef database update
```

**Pass Criteria**: 
- ✅ Rollback completes without errors
- ✅ Re-apply restores Phase 1 schema
- ✅ No data loss (existing booking data preserved)

---

## Summary Checklist

| Test Case | Status | Notes |
|-----------|--------|-------|
| 1. Organizer ID Set | ⬜ | Verify migration backfill worked |
| 2. Booking Motard Approval | ⬜ | Bob approves Charlie's booking |
| 3. Tour Organizer Approval | ⬜ | Alice approves booking for Bob's passenger |
| 4. Unauthorized Rejection | ⬜ | Charlie cannot approve own booking |
| 5. Completion with Approval | ⬜ | Effective start time override works |
| 6. Completion without Approval | ⬜ | Falls back to Tour.StartAt |
| 7. Database Audit Trail | ⬜ | All 4 fields correctly populated |
| 8. Multi-Motard Authority | ⬜ | Organizer can approve for any motard |
| 9. Frontend Display | ⬜ | UI shows new fields correctly |
| 10. Migration Rollback | ⬜ | (Optional) Migrations reversible |

---

**Testing Completed By**: __________________  
**Date**: __________________  
**All Tests Passed**: ⬜ YES  ⬜ NO (see notes)  

**Notes/Issues Found**:
_______________________________________________________
_______________________________________________________
_______________________________________________________
