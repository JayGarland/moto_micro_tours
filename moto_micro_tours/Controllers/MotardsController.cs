using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.DTOs;
using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotardsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<MotardsController> _logger;

    public MotardsController(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger<MotardsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get complete motard profile with reviews and statistics
    /// </summary>
    /// <param name="userId">Motard User ID</param>
    /// <param name="includeUpcomingTours">Include list of upcoming tours (default: false)</param>
    /// <returns>Complete motard profile including reviews</returns>
    [HttpGet("{userId}")]
    public async Task<ActionResult<MotardProfileDto>> GetMotardProfile(
        string userId,
        [FromQuery] bool includeUpcomingTours = false)
    {
        try
        {
            _logger.LogInformation("GetMotardProfile called for user {UserId}", userId);

            // Get the user with their motard profile
            var user = await _context.Users
                .Include(u => u.MotardProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return NotFound(new { error = $"User with ID {userId} not found" });
            }

            if (user.MotardProfile == null)
            {
                _logger.LogWarning("User {UserId} does not have a motard profile", userId);
                return NotFound(new { error = $"User {userId} is not registered as a motard" });
            }

            // Get reviews for this motard
            var reviews = await _context.Reviews
                .Where(r => r.MotardUserId == userId)
                .Include(r => r.Author)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    AuthorId = Guid.Parse(r.AuthorUserId),
                    AuthorName = r.Author!.UserName ?? "Unknown",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    BookingId = r.BookingId
                })
                .ToListAsync();

            // Calculate average rating from reviews
            decimal? avgRating = null;
            if (reviews.Count > 0)
            {
                avgRating = (decimal)reviews.Average(r => r.Rating);
            }

            var result = new MotardProfileDto
            {
                UserId = Guid.Parse(user.Id),
                UserName = user.UserName ?? "Unknown",
                Email = user.Email,
                Bio = user.MotardProfile.Bio,
                BikeModel = user.MotardProfile.BikeModel,
                LicenseNo = user.MotardProfile.LicenseNo,
                AvgRating = avgRating,
                TotalReviews = reviews.Count,
                Reviews = reviews
            };

            // Optionally include upcoming tours
            if (includeUpcomingTours)
            {
                var now = DateTime.UtcNow;
                var upcomingTours = await _context.TourMotards
                    .Where(tm => tm.MotardUserId == userId)
                    .Include(tm => tm.Tour)
                    .Where(tm => tm.Tour!.Status == TourStatus.Published && tm.Tour.StartAt > now)
                    .OrderBy(tm => tm.Tour!.StartAt)
                    .Select(tm => new MotardTourSummaryDto
                    {
                        TourId = tm.TourId,
                        Title = tm.Tour!.Title,
                        StartAt = tm.Tour.StartAt,
                        StartLocationText = tm.Tour.StartLocationText,
                        EndLocationText = tm.Tour.EndLocationText,
                        AvailableSeats = tm.CapacitySeats - tm.SeatsBooked,
                        PricePerSeat = tm.PricePerSeat
                    })
                    .ToListAsync();

                result.UpcomingTours = upcomingTours;

                // Also include completed tours (past tours with Closed status)
                var completedTours = await _context.TourMotards
                    .Where(tm => tm.MotardUserId == userId)
                    .Include(tm => tm.Tour)
                    .Where(tm => tm.Tour!.Status == TourStatus.Closed && tm.Tour.StartAt < now)
                    .OrderByDescending(tm => tm.Tour!.StartAt) // Most recent first
                    .Select(tm => new MotardTourSummaryDto
                    {
                        TourId = tm.TourId,
                        Title = tm.Tour!.Title,
                        StartAt = tm.Tour.StartAt,
                        StartLocationText = tm.Tour.StartLocationText,
                        EndLocationText = tm.Tour.EndLocationText,
                        AvailableSeats = tm.CapacitySeats - tm.SeatsBooked,
                        PricePerSeat = tm.PricePerSeat
                    })
                    .ToListAsync();

                result.CompletedTours = completedTours;
            }

            _logger.LogInformation(
                "Returning profile for motard {UserId} with {ReviewCount} reviews",
                userId,
                result.TotalReviews);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving motard profile for user {UserId}", userId);
            return StatusCode(500, new { error = "An error occurred while retrieving the motard profile" });
        }
    }

    /// <summary>
    /// Create a review for a motard (Stage G: Participant only, after tour completion)
    /// </summary>
    /// <param name="motardUserId">ID of the motard to review</param>
    /// <param name="request">Review details (bookingId, rating, comment)</param>
    /// <returns>Created review</returns>
    [HttpPost("{motardUserId}/reviews")]
    [Authorize(Roles = "Participant")] // Only participants can leave reviews
    public async Task<ActionResult<ReviewDto>> CreateReview(
        string motardUserId,
        [FromBody] CreateReviewDto request)
    {
        try
        {
            _logger.LogInformation(
                "Creating review for motard {MotardUserId}, booking {BookingId}",
                motardUserId,
                request.BookingId);

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "You must be logged in to create reviews" });
            }

            // Validate rating
            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { error = "Rating must be between 1 and 5" });
            }

            // Verify the motard exists
            var motard = await _context.Users
                .Include(u => u.MotardProfile)
                .FirstOrDefaultAsync(u => u.Id == motardUserId);

            if (motard == null || motard.MotardProfile == null)
            {
                _logger.LogWarning("Motard {MotardUserId} not found", motardUserId);
                return NotFound(new { error = $"Motard with ID {motardUserId} not found" });
            }

            // Load booking with tour to validate
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", request.BookingId);
                return NotFound(new { error = $"Booking with ID {request.BookingId} not found" });
            }

            // Rule 1: Must be the participant's own booking
            if (booking.ParticipantUserId != currentUser.Id)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to review booking {BookingId} they don't own",
                    currentUser.Id,
                    request.BookingId);
                return Forbid(); // 403
            }

            // Rule 2: Booking must be Paid
            if (booking.Status != BookingStatus.Paid)
            {
                _logger.LogWarning(
                    "Cannot review booking {BookingId} with status {Status}",
                    request.BookingId,
                    booking.Status);
                return BadRequest(new
                {
                    error = $"Can only review paid bookings. Current status: {booking.Status}"
                });
            }

            // Rule 3: Booking must be for the specified motard
            if (booking.TourMotard?.MotardUserId != motardUserId)
            {
                _logger.LogWarning(
                    "Booking {BookingId} is not for motard {MotardUserId}",
                    request.BookingId,
                    motardUserId);
                return BadRequest(new
                {
                    error = "This booking is not with the specified motard"
                });
            }

            // Rule 4: Tour must have already happened
            if (booking.Tour == null)
            {
                return BadRequest(new { error = "Tour information not found" });
            }

            if (booking.Tour.StartAt >= DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Cannot review booking {BookingId} - tour hasn't started yet (starts: {StartAt})",
                    request.BookingId,
                    booking.Tour.StartAt);
                return BadRequest(new
                {
                    error = "Cannot review before the tour has taken place",
                    tourStartAt = booking.Tour.StartAt
                });
            }

            // Rule 5: Check if review already exists for this booking
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookingId == request.BookingId);

            if (existingReview != null)
            {
                _logger.LogWarning(
                    "Review already exists for booking {BookingId}",
                    request.BookingId);
                return BadRequest(new
                {
                    error = "A review already exists for this booking",
                    existingReviewId = existingReview.Id
                });
            }

            // Create the review
            var review = new Review
            {
                Id = Guid.NewGuid(),
                MotardUserId = motardUserId,
                AuthorUserId = currentUser.Id,
                BookingId = request.BookingId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Review {ReviewId} created for motard {MotardUserId} by user {UserId}",
                review.Id,
                motardUserId,
                currentUser.Id);

            var response = new ReviewDto
            {
                Id = review.Id,
                AuthorId = Guid.Parse(currentUser.Id),
                AuthorName = currentUser.UserName ?? "Unknown",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                BookingId = review.BookingId
            };

            return CreatedAtAction(
                nameof(GetMotardProfile),
                new { userId = motardUserId },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for motard {MotardUserId}", motardUserId);
            return StatusCode(500, new { error = "An error occurred while creating the review" });
        }
    }
}
