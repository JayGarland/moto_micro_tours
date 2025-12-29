using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public Guid TourMotardId { get; set; }
    public string ParticipantUserId { get; set; } = null!;
    public BookingStatus Status { get; set; }
    public CancelDecision CancelDecision { get; set; }
    public bool CostSharingOptIn { get; set; }
    public string? EmergencyContact { get; set; }
    public string? MessageToMotard { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public string? DeclineReason { get; set; }

    /// <summary>
    /// RowVersion (DateTime) for optimistic concurrency control. MySQL/TIMESTAMP
    /// is used as a concurrency token with value generated on add or update.
    /// </summary>
    public DateTime RowVersion { get; set; }

    public Tour? Tour { get; set; }
    public TourMotard? TourMotard { get; set; }
    public AppUser? Participant { get; set; }
    public Payment? Payment { get; set; }
    public Payout? Payout { get; set; }
    public BookingConversation? BookingConversation { get; set; }
    public Review? Review { get; set; }
}
