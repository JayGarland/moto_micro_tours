# Option E - Quick Reference Card

## Booking Completion Flow

### Before Option E (F8 Original)
```
Booking Status: Paid
       ↓
Tour.StartAt passes
       ↓
Participant marks complete → CompletedByParticipantAtUtc
       ↓
Motard marks complete → CompletedByMotardAtUtc
       ↓
System sets CompletedAtUtc
       ↓
Reviews enabled
```

### After Option E (New)
```
Booking Status: Paid
       ↓
[OPTIONAL] Motard approves start time → ApprovedStartAtUtc
       ↓
EffectiveStartAtUtc passes
  (= ApprovedStartAtUtc ?? Tour.StartAt)
       ↓
Participant marks complete → CompletedByParticipantAtUtc
       ↓
Motard marks complete → CompletedByMotardAtUtc
       ↓
System sets CompletedAtUtc
       ↓
Reviews enabled
```

## Key Endpoints

### 1. Approve Start Time (NEW)
**POST** `/api/bookings/{id}/approve-start-time`

**Auth:** Motard only  
**Body:**
```json
{
  "approvedStartAtUtc": "2026-01-14T12:00:00Z",
  "reason": "Optional reason"
}
```

**Response:** BookingResponseDto with approval fields

### 2. Complete Booking (UPDATED)
**POST** `/api/bookings/{id}/complete`

**Auth:** Participant or Motard  
**Changed:** Now uses `EffectiveStartAtUtc` instead of `Tour.StartAt`

**Response:** BookingResponseDto with completion timestamps

## Database Fields

### Booking Table (New Columns)
| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `ApprovedStartAtUtc` | datetime(6) | Yes | Organizer-approved effective start time |
| `ApprovedStartReason` | longtext | Yes | Reason for early approval |
| `ApprovedStartByMotardAtUtc` | datetime(6) | Yes | Audit timestamp |

## Business Rules

### Approve Start Time
- ✅ Only motard can approve
- ✅ Only for Paid bookings
- ✅ Cannot be > 1 year in future
- ✅ Can be updated multiple times
- ✅ Optional reason field

### Complete Booking
- ✅ Requires Paid status
- ✅ Requires EffectiveStartAtUtc <= now
- ✅ Both parties must mark complete
- ✅ Idempotent operation
- ✅ CompletedAtUtc set when both complete

### Reviews
- ✅ Requires CompletedAtUtc != null
- ✅ No change from F8 behavior

## UI Components

### For Motard (Organizer)
**Before Approval:**
- "Approve Start Time" button visible on Paid bookings

**After Approval:**
- Green card showing approved time and reason
- "Mark as Completed" button enabled after approved time

### For Participant
**Before Approval:**
- No special UI (waits for Tour.StartAt)

**After Approval:**
- Green card showing approved time and reason (read-only)
- "Mark as Completed" button enabled after approved time

### For Both
**After One Completes:**
- "Waiting for {other party}" status

**After Both Complete:**
- "Completed" status with timestamp
- Review form enabled

## Code Snippets

### Backend: Calculate Effective Start
```csharp
var effectiveStartAtUtc = booking.ApprovedStartAtUtc ?? booking.Tour.StartAt;
if (effectiveStartAtUtc > DateTime.UtcNow) {
    return BadRequest("Cannot complete before effective start time");
}
```

### Frontend: Check Can Complete
```typescript
canMarkComplete = computed(() => {
  // ... validation checks ...
  
  const effectiveStartTime = new Date(
    bookingData.approvedStartAtUtc || bookingData.tourStartAt
  );
  const now = new Date();
  if (effectiveStartTime > now) return false;
  
  // ... more checks ...
});
```

## Testing Scenarios

### Scenario 1: Normal Flow (No Approval)
1. Booking paid at 10:00
2. Tour starts at 14:00
3. Cannot complete before 14:00 ✅
4. Can complete after 14:00 ✅

### Scenario 2: Early Approval
1. Booking paid at 10:00
2. Tour scheduled for 18:00
3. Motard approves 12:00 as start time
4. Cannot complete before 12:00 ✅
5. Can complete after 12:00 ✅ (before original 18:00)

### Scenario 3: Late Approval
1. Booking paid at 10:00
2. Tour scheduled for 14:00
3. Motard approves 20:00 as start time
4. Cannot complete before 20:00 ✅ (even though Tour.StartAt passed)

### Scenario 4: Authorization
1. Participant tries to approve start time
2. Gets 403 Forbidden ✅

### Scenario 5: Invalid Status
1. Motard tries to approve for Pending booking
2. Gets 400 Bad Request ✅

## Migration

```bash
# Already applied ✅
dotnet ef migrations add AddApprovedStartTimeToBooking
dotnet ef database update
```

## Rollback Plan

If issues arise:

```bash
# Remove migration
dotnet ef migrations remove

# Rollback database
dotnet ef database update PreviousMigrationName
```

**Note:** Rollback will lose any approved start time data.

## Monitoring

**Log Events:**
- `"Booking {id} start time approved by motard {userId}"`
- `"Cannot complete booking {id} - {type} start time hasn't passed"`
- `"Using EffectiveStartAtUtc for completion gate"`

**Metrics to Track:**
- % of bookings using approved start time
- Average time difference between Tour.StartAt and ApprovedStartAtUtc
- Completion rate before/after approval feature

## FAQ

**Q: Can participant approve start time?**  
A: No, only the motard (organizer) can approve.

**Q: Can motard approve after Tour.StartAt passes?**  
A: Yes, they can approve later times (e.g., for extended tours).

**Q: What if motard never approves?**  
A: Defaults to Tour.StartAt (F8 behavior).

**Q: Can approval be revoked?**  
A: Not implemented. Can be updated to new time, but not removed.

**Q: Does approval affect payment or refunds?**  
A: No, only affects completion eligibility.

**Q: Is approval required?**  
A: No, it's optional. Without it, behaves like F8.
