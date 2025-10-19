using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Domain.Entities;

public class Tour
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public string StartLocationText { get; set; } = null!;
    public string EndLocationText { get; set; } = null!;
    public TourStatus Status { get; set; }
    public string? Notes { get; set; }

    public ICollection<TourMotard> TourMotards { get; set; } = new List<TourMotard>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<PreBookingConversation> PreBookingConversations { get; set; } = new List<PreBookingConversation>();
}
