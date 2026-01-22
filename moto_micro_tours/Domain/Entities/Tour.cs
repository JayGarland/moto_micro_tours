using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Domain.Entities;

/// <summary>
/// Tour entity
/// TODO: Add Description field (string?) for tour overview/details
/// TODO: Add EndAt field (DateTime) to calculate tour duration
/// TODO: Add CreatedAt field (DateTime) for tracking tour creation
/// </summary>
public class Tour
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public string StartLocationText { get; set; } = null!;
    public string EndLocationText { get; set; } = null!;
    public TourStatus Status { get; set; }
    public string? Notes { get; set; }

    // Phase 1: Explicit organizer (tour creator)
    public string OrganizerUserId { get; set; } = null!;
    public AppUser Organizer { get; set; } = null!;

    public ICollection<TourMotard> TourMotards { get; set; } = new List<TourMotard>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<PreBookingConversation> PreBookingConversations { get; set; } = new List<PreBookingConversation>();
}
