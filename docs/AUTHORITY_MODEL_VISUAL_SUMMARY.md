# Hierarchical Authority Model - Quick Visual Summary

## Authority Model

```
┌─────────────────────────────────────────┐
│ TOUR ORGANIZER (Creator)                │
│ • Controls all bookings in tour         │
│ • Can approve ANY start time request    │
│ • Can override motard decisions         │
└──────────┬──────────────────────────────┘
           │
           ├─► Motard A (Booking #1, #2)
           │   • Admin over assigned bookings
           │   • Approve "in-policy" requests
           │   • Escalate to organizer for major changes
           │
           ├─► Motard B (Booking #3)
           │   • Same powers as Motard A
           │   • Independent from Motard A
           │
           └─► Participants
               • Can request changes
               • No approval authority
```

## New Workflow Comparison

### Before (Option E - Direct Set)
```
1. Motard sets ApprovedStartAtUtc ──────► Done
   (No request, no participant input)
```

### After (Request/Approve)
```
1. Participant/Motard requests ──────► Status: Pending
                                        │
                                        ├─► Within Policy?
                                        │   └─► YES: Motard approves ──► Approved
                                        │
                                        └─► Outside Policy?
                                            └─► Organizer required ──► Approved
```

## State Machine

```
       ┌─────────┐
       │  None   │ (Initial state)
       └────┬────┘
            │ Request
            ▼
       ┌─────────┐
   ┌──│ Pending │◄──┐ (Can request again)
   │  └────┬────┘   │
   │       │        │
   │       ├──Approve──► ┌──────────┐
   │       │             │ Approved │ (Terminal)
   │       │             └──────────┘
   │       │
   │       ├──Reject───► ┌──────────┐
   │       │             │ Rejected │─┘
   │       │             └──────────┘
   │       │
   │       └──Cancel───► ┌───────────┐
   │                     │ Cancelled │─┘
   │                     └───────────┘
   └─────────────────────────┘
```

## Approval Policy

```
Tour StartAt: 18:00
                    ┌─────────────────────────────┐
         BEFORE     │   Within ±24h Window        │    AFTER
    ═══════════════╪═════════════════════════════╪═══════════════►
                   │                             │
    15:00         17:00                        19:00          20:00
      ▲             │                             │              ▲
      │             │                             │              │
   Early Start  Same Day                      Same Day      Late Start
   ❌ Organizer  ✅ Motard                     ✅ Motard    ❌ Organizer
   Required      Can Approve                   Can Approve  Required
                                                            (+25h = outside)
```

## Database Changes Summary

### New Table: None (modifying existing)

### Tour Entity Changes
```diff
public class Tour
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public DateTime StartAt { get; set; }
+   public string OrganizerUserId { get; set; } // NEW: Tour creator
    ...
}
```

### Booking Entity Changes
```diff
public class Booking
{
    ...
    // Request/Approve Workflow
+   public DateTime? RequestedStartAtUtc { get; set; }
+   public string? RequestedStartReason { get; set; }
+   public DateTime? RequestedStartAtUtcTimestamp { get; set; }
+   public string? RequestedByUserId { get; set; }
    
    public DateTime? ApprovedStartAtUtc { get; set; } // EXISTS
+   public string? ApprovedByUserId { get; set; }
+   public StartTimeOverrideStatus StartTimeOverrideStatus { get; set; }
+   public string? RejectionReason { get; set; }
+   public DateTime? RejectedAtUtc { get; set; }
    ...
}
```

### New Enum
```csharp
public enum StartTimeOverrideStatus
{
    None = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
```

## Authorization Matrix

| Action | Participant | Booking Motard | Other Motard | Organizer |
|--------|:-----------:|:--------------:|:------------:|:---------:|
| Request | ✅ | ✅ | ❌ | ✅ |
| Approve (In Policy) | ❌ | ✅ | ❌ | ✅ |
| Approve (Out Policy) | ❌ | ❌ | ❌ | ✅ |
| Reject | ❌ | ✅ | ❌ | ✅ |
| Cancel Request | ✅* | ✅* | ❌ | ❌ |

*Only if they were the requester

## API Endpoints

### New Endpoints
```
POST   /api/bookings/{id}/request-start-time
POST   /api/bookings/{id}/approve-start-time  (Modified from Option E)
POST   /api/bookings/{id}/reject-start-time   (New)
DELETE /api/bookings/{id}/request-start-time  (Optional - cancel)
```

### Removed Endpoints
None (approve-start-time repurposed)

## Key Decisions Needed

1. **Policy Window:** 24 hours or different? ⏳
2. **Early Start Rule:** Always require organizer? 🚫
3. **Notifications:** Email on request/approve/reject? 📧
4. **Cancel Flow:** Allow requesters to cancel? ↩️
5. **History Tracking:** Full audit log? 📝

## Migration Impact

### Existing Data
- ✅ Tours: Set `OrganizerUserId` = first TourMotard
- ✅ Bookings: Convert existing `ApprovedStartAtUtc` to `Approved` status
- ⚠️ Breaking Change: Option E direct-set no longer supported

### Code Changes
- 🔧 3 new endpoints
- 🔧 1 modified endpoint (approve)
- 🔧 Authorization helpers
- 🔧 State machine logic
- 🔧 Frontend: Request dialog, approval buttons, status badges

## Timeline

```
Phase 1: Add Organizer (Day 1)
  ├─ Migration
  ├─ Helper methods
  └─ Tests

Phase 2: Authorization (Day 1-2)
  ├─ Policies
  ├─ Fix terminology
  └─ Tests

Phase 3: Request/Approve (Day 2-3)
  ├─ State machine
  ├─ Endpoints
  ├─ Frontend
  └─ Integration tests

Phase 4: E2E Testing (Day 3-4)
```

**Total Estimate: 3-4 days**

---

**Status:** ⏸️ Awaiting approval before implementation

**Review:** See [HIERARCHICAL_AUTHORITY_DESIGN.md](HIERARCHICAL_AUTHORITY_DESIGN.md) for full details
