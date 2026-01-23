using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;
using moto_micro_tours.DTOs;
using System.Security.Claims;

namespace moto_micro_tours.Controllers;
// TODO: For reviews, reviewer and target both side can edit their comment, like update, delete, etc
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(AppDbContext db, ILogger<ReviewsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/reviews - Create a review for a completed booking
    /// Validates: booking exists, user is reviewer, tour completed, booking paid, no duplicate review
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // 1. Validate booking exists and is paid
        var booking = await _db.Bookings
            .Include(b => b.TourMotard)
            .Include(b => b.Tour)
                .ThenInclude(t => t.TourMotards)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null)
            return NotFound(new { error = "Booking not found" });

        // 2. Verify user is participant or motard (both can review)
        var isParticipant = booking.ParticipantUserId == userId;
        var isMotard = booking.Tour.TourMotards.Any(tm => tm.MotardUserId == userId);

        if (!isParticipant && !isMotard)
        {
            _logger.LogWarning("User {UserId} attempted to review booking {BookingId} but is neither participant nor motard",
                userId, dto.BookingId);
            return Forbid();
        }

        // 3. Verify booking is paid
        if (booking.Status != BookingStatus.Paid)
            return BadRequest(new { error = "Can only review paid bookings" });

        // 4. Verify booking has been completed by both parties
        if (!booking.CompletedAtUtc.HasValue)
            return BadRequest(new { error = "Cannot review until booking is marked as completed by both parties" });

        // 5. Validate TargetType and TargetId
        string targetId;
        if (dto.TargetType == ReviewTargetType.Tour)
        {
            targetId = booking.TourId.ToString();
        }
        else if (dto.TargetType == ReviewTargetType.User)
        {
            // Allow reviewing motards (by participants) or participants (by motards)
            var isValidTarget = false;

            // Check if target is a motard on this tour
            var isTargetMotardOnTour = booking.Tour.TourMotards
                .Any(tm => tm.MotardUserId == dto.TargetId);

            // Check if target is the participant
            var isTargetParticipant = booking.ParticipantUserId == dto.TargetId;

            if (isParticipant && isTargetMotardOnTour)
            {
                // Participant can review motards
                isValidTarget = true;
            }
            else if (isMotard && isTargetParticipant)
            {
                // Motard can review participant
                isValidTarget = true;
            }

            if (!isValidTarget)
                return BadRequest(new { error = "Invalid review target for this booking" });

            targetId = dto.TargetId;
        }
        else
        {
            return BadRequest(new { error = "Invalid TargetType" });
        }

        // 6. Check for duplicate review (unique constraint: BookingId + ReviewerUserId + TargetType + TargetId)
        var existingReview = await _db.Reviews
            .FirstOrDefaultAsync(r =>
                r.BookingId == dto.BookingId &&
                r.ReviewerUserId == userId &&
                r.TargetType == dto.TargetType &&
                r.TargetId == targetId);

        if (existingReview != null)
            return Conflict(new { error = "You have already reviewed this target for this booking" });

        // 7. Validate rating range
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest(new { error = "Rating must be between 1 and 5" });

        // 8. Validate comment length
        if (dto.Comment?.Length > 2000)
            return BadRequest(new { error = "Comment must be 2000 characters or less" });

        // 9. Create review
        var review = new Review
        {
            BookingId = dto.BookingId,
            ReviewerUserId = userId,
            TargetType = dto.TargetType,
            TargetId = targetId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Reviews.Add(review);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("Duplicate") == true ||
                  ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            _logger.LogWarning("Duplicate review detected for BookingId={BookingId}", dto.BookingId);

            // Race condition: another request created review between check and insert
            // Reload to get the existing review and return it (idempotent behavior)
            var existingReviewReloaded = await _db.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Replies)
                .FirstOrDefaultAsync(r =>
                    r.BookingId == dto.BookingId &&
                    r.ReviewerUserId == userId &&
                    r.TargetType == dto.TargetType &&
                    r.TargetId == targetId);

            if (existingReviewReloaded != null)
            {
                _logger.LogInformation(
                    "Returning existing review after race condition: ReviewId={ReviewId}",
                    existingReviewReloaded.Id);

                // Return existing review as if we created it (idempotent)
                return CreatedAtAction(
                    nameof(GetReview),
                    new { id = existingReviewReloaded.Id },
                    MapReviewToDto(existingReviewReloaded));
            }

            // If we still can't find it, return conflict
            return Conflict(new { error = "You have already reviewed this target for this booking" });
        }

        _logger.LogInformation(
            "Review created: ReviewId={ReviewId}, BookingId={BookingId}, TargetType={TargetType}, TargetId={TargetId}",
            review.Id, review.BookingId, review.TargetType, review.TargetId);

        // 9. Return created review with details
        var createdReview = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Replies)
            .FirstOrDefaultAsync(r => r.Id == review.Id);

        // Clients receive a 201 response, a Location header pointing to / api / reviews /{ id}, and the full review object in the response body.This is a RESTful pattern for resource creation.
        return CreatedAtAction(
            nameof(GetReview),
            new { id = review.Id },
            MapReviewToDto(createdReview!));
    }

    /// <summary>
    /// GET /api/reviews/{id} - Get a single review by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReview(Guid id)
    {
        var review = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Replies)
            .ThenInclude(rr => rr.Author)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            return NotFound();

        return Ok(MapReviewToDto(review));
    }

    /// <summary>
    /// POST /api/reviews/{id}/replies - Add a reply to a review
    /// Only the author of the review target can reply (e.g., motard replying to their review)
    /// </summary>
    [HttpPost("{id}/replies")]
    public async Task<IActionResult> AddReply(Guid id, [FromBody] CreateReviewReplyDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // 1. Get review
        var review = await _db.Reviews
            .Include(r => r.Replies)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            return NotFound(new { error = "Review not found" });

        // 2. Verify user is either the reviewer (can add updates) or the target (can reply)
        bool isReviewer = review.ReviewerUserId == userId;
        bool isTarget = review.TargetId == userId && review.TargetType == ReviewTargetType.User;

        if (!isReviewer && !isTarget)
            return Forbid(); // Only reviewer or review target can add replies

        // 3. Validate message length
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { error = "Reply message cannot be empty" });

        if (dto.Message.Length > 2000)
            return BadRequest(new { error = "Reply message must be 2000 characters or less" });

        // 4. Create reply
        var reply = new ReviewReply
        {
            ReviewId = id,
            AuthorUserId = userId,
            Message = dto.Message,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ReviewReplies.Add(reply);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Review reply created: ReplyId={ReplyId}, ReviewId={ReviewId}, AuthorUserId={AuthorUserId}",
            reply.Id, reply.ReviewId, reply.AuthorUserId);

        // 5. Return updated review with all replies
        var updatedReview = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Replies)
            .ThenInclude(rr => rr.Author)
            .FirstOrDefaultAsync(r => r.Id == id);

        return Ok(MapReviewToDto(updatedReview!));
    }

    /// <summary>
    /// GET /api/bookings/{bookingId}/reviews - Get all reviews for a booking
    /// </summary>
    [HttpGet("/api/bookings/{bookingId}/reviews")]
    public async Task<IActionResult> GetBookingReviews(Guid bookingId)
    {
        // Verify booking exists
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking == null)
            return NotFound(new { error = "Booking not found" });

        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Replies)
            .ThenInclude(rr => rr.Author)
            .Where(r => r.BookingId == bookingId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        return Ok(reviews.Select(MapReviewToDto));
    }

    /// <summary>
    /// GET /api/tours/{tourId}/reviews - Get all reviews for a tour
    /// </summary>
    [HttpGet("/api/tours/{tourId}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTourReviews(Guid tourId)
    {
        // Verify tour exists
        var tour = await _db.Tours.FindAsync(tourId);
        if (tour == null)
            return NotFound(new { error = "Tour not found" });

        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Replies)
            .ThenInclude(rr => rr.Author)
            .Where(r => r.TargetType == ReviewTargetType.Tour && r.TargetId == tourId.ToString())
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        return Ok(reviews.Select(MapReviewToDto));
    }

    /// <summary>
    /// GET /api/users/{userId}/reviews - Get all reviews for a user (motard)
    /// </summary>
    [HttpGet("/api/users/{userId}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserReviews(string userId)
    {
        // Verify user exists
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found" });

        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Replies)
            .ThenInclude(rr => rr.Author)
            .Where(r => r.TargetType == ReviewTargetType.User && r.TargetId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        return Ok(reviews.Select(MapReviewToDto));
    }

    // Helper to map Review entity to DTO
    private ReviewDto MapReviewToDto(Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            BookingId = review.BookingId,
            ReviewerUserId = review.ReviewerUserId,
            ReviewerUsername = review.Reviewer?.UserName ?? "Unknown",
            TargetType = review.TargetType,
            TargetId = review.TargetId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAtUtc = review.CreatedAtUtc,
            Replies = review.Replies?.Select(rr => new ReviewReplyDto
            {
                Id = rr.Id,
                ReviewId = rr.ReviewId,
                AuthorUserId = rr.AuthorUserId,
                AuthorUsername = rr.Author?.UserName ?? "Unknown",
                Message = rr.Message,
                CreatedAtUtc = rr.CreatedAtUtc
            }).ToList() ?? new List<ReviewReplyDto>()
        };
    }
}
