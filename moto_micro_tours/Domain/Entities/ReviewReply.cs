namespace moto_micro_tours.Domain.Entities;

/// <summary>
/// Represents a reply/update to a review.
/// Only the original review author can add replies to their review.
/// Replies do not change the star rating.
/// </summary>
public class ReviewReply
{
    public Guid Id { get; set; }

    /// <summary>
    /// The review this reply belongs to
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// The user who wrote this reply (must be the original review author)
    /// </summary>
    public string AuthorUserId { get; set; } = null!;

    /// <summary>
    /// The reply message/text
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// When the reply was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public Review? Review { get; set; }
    public AppUser? Author { get; set; }
}
