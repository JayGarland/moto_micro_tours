using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.DTOs;
using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ToursController> _logger;

    public ToursController(AppDbContext context, ILogger<ToursController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all tours with optional filters
    /// </summary>
    /// <param name="date">Filter by start date (format: yyyy-MM-dd)</param>
    /// <param name="area">Filter by location text (case-insensitive contains)</param>
    /// <param name="priceMax">Filter by maximum price per seat</param>
    /// <returns>List of tours matching the criteria</returns>
    [HttpGet]
    public async Task<ActionResult<List<TourListDto>>> GetTours(
        [FromQuery] DateTime? date = null,
        [FromQuery] string? area = null,
        [FromQuery] decimal? priceMax = null)
    {
        try
        {
            _logger.LogInformation(
                "GetTours called with filters - Date: {Date}, Area: {Area}, PriceMax: {PriceMax}",
                date, area, priceMax);

            // Start with Published tours only (for public API)
            var query = _context.Tours
                .Include(t => t.TourMotards)
                    .ThenInclude(tm => tm.Motard!)
                    .ThenInclude(u => u.MotardProfile)
                .Where(t => t.Status == TourStatus.Published);

            // Apply date filter (tours starting on or after the specified date)
            if (date.HasValue)
            {
                var startOfDay = date.Value.Date;
                var endOfDay = startOfDay.AddDays(1);
                query = query.Where(t => t.StartAt >= startOfDay && t.StartAt < endOfDay);
            }

            // Apply area filter (simple text search in start or end location)
            if (!string.IsNullOrWhiteSpace(area))
            {
                var areaLower = area.ToLower();
                query = query.Where(t =>
                    t.StartLocationText.ToLower().Contains(areaLower) ||
                    t.EndLocationText.ToLower().Contains(areaLower));
            }

            // Apply price filter (at least one motard has price <= priceMax)
            if (priceMax.HasValue)
            {
                query = query.Where(t => t.TourMotards.Any(tm => tm.PricePerSeat <= priceMax.Value));
            }

            var tours = await query
                .OrderBy(t => t.StartAt)
                .ToListAsync();

            var result = tours.Select(t => new TourListDto
            {
                Id = t.Id,
                Title = t.Title,
                StartAt = t.StartAt,
                StartLocationText = t.StartLocationText,
                EndLocationText = t.EndLocationText,
                Status = t.Status.ToString(),
                Notes = t.Notes,
                TotalCapacity = t.TourMotards.Sum(tm => tm.CapacitySeats),
                TotalSeatsBooked = t.TourMotards.Sum(tm => tm.SeatsBooked),
                MinPrice = t.TourMotards.Any() ? t.TourMotards.Min(tm => tm.PricePerSeat) : 0,
                MaxPrice = t.TourMotards.Any() ? t.TourMotards.Max(tm => tm.PricePerSeat) : 0,
                MotardCount = t.TourMotards.Count,
                Motards = t.TourMotards.Select(tm => new TourMotardSummaryDto
                {
                    TourMotardId = tm.Id,
                    MotardUserId = Guid.Parse(tm.MotardUserId),
                    MotardName = tm.Motard?.UserName ?? "Unknown",
                    CapacitySeats = tm.CapacitySeats,
                    SeatsBooked = tm.SeatsBooked,
                    PricePerSeat = tm.PricePerSeat
                }).ToList()
            }).ToList();

            _logger.LogInformation("Returning {Count} tours", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tours");
            return StatusCode(500, new { error = "An error occurred while retrieving tours" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific tour
    /// </summary>
    /// <param name="id">Tour ID</param>
    /// <returns>Detailed tour information including all motards with their profiles</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<TourDetailDto>> GetTourById(Guid id)
    {
        try
        {
            _logger.LogInformation("GetTourById called for tour {TourId}", id);

            var tour = await _context.Tours
                .Include(t => t.TourMotards)
                    .ThenInclude(tm => tm.Motard)
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    Tour = t,
                    Motards = t.TourMotards.Select(tm => new
                    {
                        TourMotard = tm,
                        User = tm.Motard,
                        Profile = tm.Motard!.MotardProfile
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (tour == null)
            {
                _logger.LogWarning("Tour {TourId} not found", id);
                return NotFound(new { error = $"Tour with ID {id} not found" });
            }

            var result = new TourDetailDto
            {
                Id = tour.Tour.Id,
                Title = tour.Tour.Title,
                StartAt = tour.Tour.StartAt,
                StartLocationText = tour.Tour.StartLocationText,
                EndLocationText = tour.Tour.EndLocationText,
                Status = tour.Tour.Status.ToString(),
                Notes = tour.Tour.Notes,
                Motards = tour.Motards.Select(m => new TourMotardDetailDto
                {
                    TourMotardId = m.TourMotard.Id,
                    MotardUserId = Guid.Parse(m.TourMotard.MotardUserId),
                    MotardName = m.User?.UserName ?? "Unknown",
                    MotardBio = m.Profile?.Bio,
                    BikeModel = m.Profile?.BikeModel,
                    AvgRating = (float?)(m.Profile?.AvgRating),
                    CompletedTours = m.Profile?.CompletedTours ?? 0,
                    CapacitySeats = m.TourMotard.CapacitySeats,
                    SeatsBooked = m.TourMotard.SeatsBooked,
                    PricePerSeat = m.TourMotard.PricePerSeat
                }).ToList()
            };

            _logger.LogInformation("Returning tour {TourId} with {MotardCount} motards", id, result.Motards.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tour {TourId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the tour" });
        }
    }
}
