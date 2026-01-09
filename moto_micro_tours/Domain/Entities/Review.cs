using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Domain.Entities;

/// <summary>
/// Generic review entity that can target either a Tour or a User (motard/participant).
/// Enforces one review per booking per reviewer per target via unique constraint.
/// </summary>
public class Review
{
    public Guid Id { get; set; }

    /// <summary>
    /// The booking this review is associated with
    /// </summary>
    public Guid BookingId { get; set; }

    /// <summary>
    /// The user who wrote this review
    /// </summary>
    public string ReviewerUserId { get; set; } = null!;

    /// <summary>
    /// What type of entity is being reviewed (Tour or User)
    /// </summary>
    public ReviewTargetType TargetType { get; set; }

    /// <summary>
    /// The ID of the entity being reviewed (TourId or UserId as string)
    /// </summary>
    public string TargetId { get; set; } = null!;

    /// <summary>
    /// Star rating (1-5)
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Optional comment/text
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// When the review was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public Booking? Booking { get; set; }
    public AppUser? Reviewer { get; set; }
    public ICollection<ReviewReply> Replies { get; set; } = new List<ReviewReply>();
}
