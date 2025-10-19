namespace moto_micro_tours.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }
    public string MotardUserId { get; set; } = null!;
    public string AuthorUserId { get; set; } = null!;
    public Guid BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public AppUser? Motard { get; set; }
    public AppUser? Author { get; set; }
    public Booking? Booking { get; set; }
}
