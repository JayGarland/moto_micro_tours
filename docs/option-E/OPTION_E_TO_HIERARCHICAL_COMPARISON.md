# Option E → Hierarchical Authority Model: What Changes?

**Date:** January 18, 2026

---

## Side-by-Side Comparison

### Data Model

| Aspect | Option E (Current) | Hierarchical Model (New) |
|--------|-------------------|-------------------------|
| **Organizer Concept** | ❌ None (implicit) | ✅ Explicit `Tour.OrganizerUserId` |
| **Request Field** | ❌ None | ✅ `Booking.RequestedStartAtUtc` |
| **Status Tracking** | ❌ None | ✅ `StartTimeOverrideStatus` enum |
| **Approval History** | ⚠️ Timestamp only | ✅ Who approved + role |
| **Rejection Support** | ❌ None | ✅ Rejection reason + timestamp |

### User Flows

#### **Scenario 1: Early Tour Completion**

**Option E (Current):**
```
1. Tour finishes at 15:00 (scheduled 18:00)
2. Motard directly sets ApprovedStartAtUtc = 15:00
3. Participant can now mark complete
```
❌ Participant has no input
❌ No record of why time changed

**Hierarchical Model (New):**
```
1. Tour finishes at 15:00 (scheduled 18:00)
2. Participant requests: "Finished early"
3. System: "Early start requires organizer approval"
4. Organizer reviews and approves
5. Participant can now mark complete
```
✅ Participant can request
✅ Policy enforced (early = organizer)
✅ Audit trail preserved

#### **Scenario 2: Minor Time Adjustment (+2 hours)**

**Option E (Current):**
```
1. Motard sets ApprovedStartAtUtc = 20:00
2. Done
```

**Hierarchical Model (New):**
```
1. Motard or participant requests 20:00
2. System: "Within policy - motard can approve"
3. Motard approves (no organizer needed)
4. Done
```
✅ Same outcome, better process

### Authorization

| Check | Option E | Hierarchical Model |
|-------|----------|-------------------|
| **Who can set/request time?** | Booking's motard only | Participant, motard, organizer |
| **Who can approve?** | Booking's motard | Depends on policy:<br>• In-policy: Motard<br>• Out-policy: Organizer |
| **Escalation to organizer?** | ❌ Never | ✅ For major changes |
| **Terminology accuracy?** | ❌ "Organizer" but checks motard | ✅ Explicit distinction |

### Endpoints

#### **Option E (Current):**
```
POST /api/bookings/{id}/approve-start-time
  - Direct set by motard
  - No request concept
  - Immediate effect
```

#### **Hierarchical Model (New):**
```
POST /api/bookings/{id}/request-start-time
  - Creates request (Pending status)
  - Anyone can request
  
POST /api/bookings/{id}/approve-start-time
  - Approves pending request
  - Policy-based authorization
  - State transition
  
POST /api/bookings/{id}/reject-start-time
  - Rejects pending request
  - Preserves audit trail
```

### Database Fields

```diff
Tour Entity:
+   OrganizerUserId (NEW)

Booking Entity:
+   RequestedStartAtUtc (NEW)
+   RequestedStartReason (NEW)
+   RequestedStartAtUtcTimestamp (NEW)
+   RequestedByUserId (NEW)
    ApprovedStartAtUtc (EXISTS - unchanged)
    ApprovedStartReason (EXISTS - unchanged)
    ApprovedStartByMotardAtUtc (EXISTS - rename to ApprovedAtUtc)
+   ApprovedByUserId (NEW)
+   StartTimeOverrideStatus (NEW)
+   RejectionReason (NEW)
+   RejectedAtUtc (NEW)
```

### Code Changes Summary

#### **Files Modified:**

1. **Domain/Entities/Tour.cs**
   - Add `OrganizerUserId` field

2. **Domain/Entities/Booking.cs**
   - Add 8 new fields
   - Add enum property

3. **Domain/Enums/DomainEnums.cs**
   - Add `StartTimeOverrideStatus` enum

4. **Controllers/BookingsController.cs**
   - Modify `ApproveStartTime()` endpoint
   - Add `RequestStartTime()` endpoint
   - Add `RejectStartTime()` endpoint
   - Update authorization checks

5. **DTOs/BookingDtos.cs**
   - Add `RequestStartTimeRequest`
   - Add `ApproveStartTimeRequest` (modify existing)
   - Add `RejectStartTimeRequest`
   - Update `BookingResponseDto` with new fields

6. **Services/AuthorizationHelpers.cs** (NEW)
   - Create helper methods

7. **Frontend (Multiple Files)**
   - Request dialog component
   - Approval/rejection buttons
   - Status badge display
   - Service methods

#### **Files Created:**

- `Migrations/YYYYMMDD_AddOrganizerAndRequestApprove.cs`
- `Services/AuthorizationHelpers.cs`
- `Services/IAuthorizationHelpers.cs`

### Migration Path

#### **Existing Tours:**
```sql
-- Set organizer = first motard added
UPDATE Tours t
SET t.OrganizerUserId = (
    SELECT tm.MotardUserId 
    FROM TourMotards tm 
    WHERE tm.TourId = t.Id 
    ORDER BY tm.RowVersion ASC 
    LIMIT 1
);
```

#### **Existing Approved Times:**
```sql
-- Convert Option E direct-sets to Approved status
UPDATE Bookings b
INNER JOIN TourMotards tm ON b.TourMotardId = tm.Id
INNER JOIN Tours t ON tm.TourId = t.Id
SET 
    b.StartTimeOverrideStatus = 2, -- Approved
    b.ApprovedByUserId = t.OrganizerUserId,
    b.ApprovedStartReason = COALESCE(b.ApprovedStartReason, 'Migrated from Option E')
WHERE b.ApprovedStartAtUtc IS NOT NULL;
```

### Backward Compatibility

#### **Breaking Changes:**

1. ❌ **Direct-set no longer works**
   - Old: Motard calls `approve-start-time` with time
   - New: Must call `request-start-time` first

2. ❌ **Authorization stricter**
   - Old: Any motard on booking
   - New: Policy-based (may require organizer)

3. ⚠️ **Frontend updates required**
   - New request dialog needed
   - Approval flow more complex

#### **Compatible Changes:**

1. ✅ **Completion gate unchanged**
   - `EffectiveStartAtUtc` calculation same
   - Mutual handshake preserved

2. ✅ **Existing data preserved**
   - Migrations handle old approvals
   - No data loss

### Testing Migration

#### **Test Case 1: Existing Approved Booking**
```
Given: Booking has ApprovedStartAtUtc = 15:00 (Option E)
After Migration:
  - StartTimeOverrideStatus = Approved ✅
  - ApprovedByUserId = Organizer ✅
  - ApprovedStartAtUtc = 15:00 (preserved) ✅
  - Completion still works ✅
```

#### **Test Case 2: New Request Flow**
```
Given: New booking (no approved time)
Flow:
  1. Participant requests 15:00 → Pending ✅
  2. System checks policy → Organizer required ✅
  3. Motard tries to approve → 403 Forbidden ✅
  4. Organizer approves → Approved ✅
  5. EffectiveStartAtUtc = 15:00 ✅
```

### Risk Analysis

#### **High Risk Areas:**

1. **Migration Script**
   - Setting wrong organizer breaks authorization
   - **Mitigation:** Test on staging with real data

2. **State Machine**
   - Complex transitions could have bugs
   - **Mitigation:** Unit test all transitions

3. **Policy Logic**
   - Incorrect deviation calculation
   - **Mitigation:** Extensive unit tests with edge cases

#### **Medium Risk Areas:**

1. **Authorization Checks**
   - Missing organizer vs motard distinction
   - **Mitigation:** Authorization matrix tests

2. **Frontend Complexity**
   - More UI states to handle
   - **Mitigation:** Component tests + E2E

### Rollback Plan

#### **If Major Issues Found:**

1. **Database Rollback:**
   ```sql
   -- Remove new columns (data loss!)
   ALTER TABLE Tours DROP COLUMN OrganizerUserId;
   ALTER TABLE Bookings DROP COLUMN RequestedStartAtUtc;
   -- ... (all new columns)
   
   -- Restore to Option E state
   dotnet ef database update PreviousMigrationName
   ```

2. **Code Rollback:**
   ```bash
   git revert <commit-range>
   git push origin main
   ```

3. **Frontend Rollback:**
   - Deploy previous version
   - Disable request dialog feature flag

#### **Partial Rollback (Keep Organizer, Remove Request/Approve):**

Possible if only Task A deployed:
- Keep `OrganizerUserId`
- Revert request/approve workflow
- Keep Option E direct-set

### Deployment Strategy

#### **Recommended: Phased Rollout**

**Phase 1 (Day 1):**
- Deploy Task A (Organizer) to staging
- Run migration
- Verify organizer assignment
- No user-facing changes

**Phase 2 (Day 2):**
- Deploy Task B (Authorization) to staging
- Test all authorization checks
- Still no user-facing changes

**Phase 3 (Day 3):**
- Deploy Task C (Request/Approve) to staging
- Full E2E testing
- User acceptance testing

**Phase 4 (Day 4):**
- Production deployment with feature flag
- Gradual rollout (10% → 50% → 100%)
- Monitor error rates

### Success Metrics

#### **Post-Deployment Monitoring:**

1. **Authorization Failures**
   - Target: < 1% 403 errors
   - Alert if > 5%

2. **Organizer Escalations**
   - Track: % of requests requiring organizer
   - Expected: ~30-40% (early starts)

3. **Approval Time**
   - Track: Time from request → approval
   - Target: < 2 hours median

4. **State Machine Errors**
   - Target: 0 invalid state transitions
   - Alert immediately

### Communication Plan

#### **Developer Team:**
- Design doc review (this document)
- Code walkthrough session
- Testing strategy alignment

#### **Product Team:**
- Policy parameter decisions
- UX flow approval
- User communication draft

#### **Users:**
- In-app notification: "New request/approve workflow"
- Help center article
- Tutorial video (optional)

---

## Decision Point

**Before proceeding, confirm:**

1. ✅ Hierarchical model is the desired direction
2. ✅ 24-hour policy window is acceptable (or specify alternative)
3. ✅ Early starts always require organizer (or specify alternative)
4. ✅ Breaking changes from Option E are acceptable
5. ✅ 3-4 day implementation timeline is acceptable

**Once confirmed, implementation can begin with Task A.**
