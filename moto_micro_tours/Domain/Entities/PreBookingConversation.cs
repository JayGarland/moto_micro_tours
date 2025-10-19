namespace moto_micro_tours.Domain.Entities;

public class PreBookingConversation
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string MotardUserId { get; set; } = null!;
    public string ParticipantUserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Tour? Tour { get; set; }
    public AppUser? Motard { get; set; }
    public AppUser? Participant { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
