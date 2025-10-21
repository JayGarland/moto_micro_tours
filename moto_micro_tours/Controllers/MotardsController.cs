using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.DTOs;
using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotardsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<MotardsController> _logger;

    public MotardsController(AppDbContext context, ILogger<MotardsController> logger)
    {
        _context = context;
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

            var result = new MotardProfileDto
            {
                UserId = Guid.Parse(user.Id),
                UserName = user.UserName ?? "Unknown",
                Email = user.Email,
                Bio = user.MotardProfile.Bio,
                BikeModel = user.MotardProfile.BikeModel,
                LicenseNo = user.MotardProfile.LicenseNo,
                AvgRating = user.MotardProfile.AvgRating,
                CompletedTours = user.MotardProfile.CompletedTours,
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
}
