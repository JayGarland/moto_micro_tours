namespace moto_micro_tours.Domain.Entities;

public class RateLimitCounter
{
    public Guid Id { get; set; }
    public string ParticipantUserId { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int NewConversationsCount { get; set; }

    public AppUser? Participant { get; set; }
}
