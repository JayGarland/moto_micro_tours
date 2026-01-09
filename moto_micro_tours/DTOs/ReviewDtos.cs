using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.DTOs;

/// <summary>
/// DTO for creating a new review
/// </summary>
public class CreateReviewDto
{
    public Guid BookingId { get; set; }
    public ReviewTargetType TargetType { get; set; }
    public string TargetId { get; set; } = string.Empty; // TourId (as string) or UserId
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
}

/// <summary>
/// DTO for adding a reply to a review
/// </summary>
public class CreateReviewReplyDto
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for returning review data with replies
/// </summary>
public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string ReviewerUserId { get; set; } = string.Empty;
    public string ReviewerUsername { get; set; } = string.Empty;
    public ReviewTargetType TargetType { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<ReviewReplyDto> Replies { get; set; } = new();
}

/// <summary>
/// DTO for review reply
/// </summary>
public class ReviewReplyDto
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
