namespace moto_micro_tours.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid? PreBookingConversationId { get; set; }
    public Guid? BookingConversationId { get; set; }
    public string SenderUserId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; }

    public PreBookingConversation? PreBookingConversation { get; set; }
    public BookingConversation? BookingConversation { get; set; }
    public AppUser? Sender { get; set; }
}
