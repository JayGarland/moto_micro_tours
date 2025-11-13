using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.DTOs;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger<BookingsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Create a new booking request (Stage F: Authenticated users only, typically Participants)
    /// </summary>
    /// <param name="request">Booking request details</param>
    /// <returns>Created booking with Pending status</returns>
    [HttpPost("request")]
    [Authorize] // Any authenticated user can create a booking request
    public async Task<ActionResult<BookingResponseDto>> CreateBookingRequest(
        [FromBody] CreateBookingRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Creating booking request for tour {TourId}, motard {TourMotardId}",
                request.TourId, request.TourMotardId);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.EmergencyContact))
            {
                return BadRequest(new { error = "Emergency contact is required" });
            }

            // Load tour with motards
            var tour = await _context.Tours
                .Include(t => t.TourMotards)
                    .ThenInclude(tm => tm.Motard)
                .FirstOrDefaultAsync(t => t.Id == request.TourId);

            if (tour == null)
            {
                _logger.LogWarning("Tour {TourId} not found", request.TourId);
                return NotFound(new { error = $"Tour with ID {request.TourId} not found" });
            }

            // Verify tour is published
            if (tour.Status != TourStatus.Published)
            {
                _logger.LogWarning("Tour {TourId} is not published (status: {Status})",
                    request.TourId, tour.Status);
                return BadRequest(new { error = $"Tour is not available for booking (status: {tour.Status})" });
            }

            // Verify tour motard exists in this tour
            var tourMotard = tour.TourMotards
                .FirstOrDefault(tm => tm.Id == request.TourMotardId);

            if (tourMotard == null)
            {
                _logger.LogWarning(
                    "TourMotard {TourMotardId} not found in tour {TourId}",
                    request.TourMotardId, request.TourId);
                return BadRequest(new { error = "Selected motard is not part of this tour" });
            }

            // Check if seats are available (pre-check, actual guard happens on approval)
            if (tourMotard.SeatsBooked >= tourMotard.CapacitySeats)
            {
                _logger.LogWarning(
                    "No seats available for TourMotard {TourMotardId} ({SeatsBooked}/{CapacitySeats})",
                    request.TourMotardId, tourMotard.SeatsBooked, tourMotard.CapacitySeats);
                return BadRequest(new { error = "No seats available for this motard" });
            }

            // Stage F: Get the current authenticated user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Booking request attempt without authentication");
                return Unauthorized(new { error = "You must be logged in to create a booking" });
            }

            _logger.LogInformation(
                "User {UserId} ({Username}) creating booking request",
                currentUser.Id, currentUser.UserName);

            // Create the booking
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                TourId = request.TourId,
                TourMotardId = request.TourMotardId,
                ParticipantUserId = currentUser.Id,
                Status = BookingStatus.Pending,
                EmergencyContact = request.EmergencyContact,
                CostSharingOptIn = request.CostSharingOptIn,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking {BookingId} created successfully with status Pending",
                booking.Id);

            var response = new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = tour.Id,
                TourTitle = tour.Title,
                TourMotardId = tourMotard.Id,
                MotardName = tourMotard.Motard?.UserName ?? "Unknown",
                ParticipantId = Guid.Parse(currentUser.Id),
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt
            };

            return CreatedAtAction(
                nameof(GetBookingById),
                new { id = booking.Id },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking request");
            return StatusCode(500, new { error = "An error occurred while creating the booking" });
        }
    }

    /// <summary>
    /// Approve a booking request (Stage F: Only the assigned motard can approve)
    /// Sets status to Approved and creates 6-hour payment window
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Updated booking with Approved status and expiry time</returns>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Motard")] // Must be a Motard, and specifically the one assigned to this booking
    public async Task<ActionResult<BookingResponseDto>> ApproveBooking(Guid id)
    {
        try
        {
            _logger.LogInformation("Approving booking {BookingId}", id);

            // Load booking with related entities
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm.Motard)
                .Include(b => b.Participant)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", id);
                return NotFound(new { error = $"Booking with ID {id} not found" });
            }

            // Verify booking is in Pending status
            if (booking.Status != BookingStatus.Pending)
            {
                _logger.LogWarning(
                    "Booking {BookingId} cannot be approved (current status: {Status})",
                    id, booking.Status);
                return BadRequest(new
                {
                    error = $"Booking cannot be approved. Current status: {booking.Status}"
                });
            }

            // Verify tour is still Published
            if (booking.Tour?.Status != TourStatus.Published)
            {
                _logger.LogWarning(
                    "Tour {TourId} is not published (status: {Status})",
                    booking.TourId, booking.Tour?.Status);
                return BadRequest(new
                {
                    error = $"Tour is no longer available for booking (status: {booking.Tour?.Status})"
                });
            }

            // Check capacity guard: seats still available?
            if (booking.TourMotard == null)
            {
                return BadRequest(new { error = "TourMotard not found" });
            }

            if (booking.TourMotard.SeatsBooked >= booking.TourMotard.CapacitySeats)
            {
                _logger.LogWarning(
                    "No seats available for TourMotard {TourMotardId} ({SeatsBooked}/{CapacitySeats})",
                    booking.TourMotardId,
                    booking.TourMotard.SeatsBooked,
                    booking.TourMotard.CapacitySeats);
                return BadRequest(new
                {
                    error = "No seats available. All seats have been booked."
                });
            }

            // Stage F - Step 3B: Verify current user is the assigned motard
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Approval attempt without authentication for booking {BookingId}", id);
                return Unauthorized(new { error = "You must be logged in to approve bookings" });
            }

            // Only the assigned motard can approve their own bookings
            if (booking.TourMotard.MotardUserId != currentUser.Id)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to approve booking {BookingId} assigned to motard {MotardUserId}",
                    currentUser.Id, id, booking.TourMotard.MotardUserId);
                return Forbid(); // 403 Forbidden - user is authenticated but not authorized
            }

            _logger.LogInformation(
                "Motard {MotardUserId} ({Username}) is authorized to approve booking {BookingId}",
                currentUser.Id, currentUser.UserName, id);

            // Approve the booking
            booking.Status = BookingStatus.Approved;
            booking.ApprovedAt = DateTime.UtcNow;
            booking.ExpiresAt = DateTime.UtcNow.AddHours(6); // 6-hour payment window

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking {BookingId} approved. Payment must be completed by {ExpiresAt}",
                booking.Id, booking.ExpiresAt);

            var response = new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? "Unknown",
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard.Motard?.UserName ?? "Unknown",
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while approving the booking" });
        }
    }

    /// <summary>
    /// Get complete booking details by ID
    /// Shows booking status, tour info, payment, and payout details
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Complete booking information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDetailDto>> GetBookingById(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting booking details for {BookingId}", id);

            // Load booking with all related entities
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm!.Motard)
                .Include(b => b.Participant)
                .Include(b => b.Payment)
                .Include(b => b.Payout)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", id);
                return NotFound(new { error = $"Booking with ID {id} not found" });
            }

            var response = new BookingDetailDto
            {
                BookingId = booking.Id,

                // Tour information
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? "Unknown",
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                StartLocationText = booking.Tour?.StartLocationText ?? string.Empty,
                EndLocationText = booking.Tour?.EndLocationText ?? string.Empty,

                // Motard information
                TourMotardId = booking.TourMotardId,
                MotardUserId = booking.TourMotard?.MotardUserId ?? string.Empty,
                MotardName = booking.TourMotard?.Motard?.UserName ?? "Unknown",
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,

                // Participant information
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                ParticipantName = booking.Participant?.UserName ?? "Unknown",

                // Booking details
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                PaidAt = booking.PaidAt,
                CancelledAt = booking.CancelledAt,
                CancellationReason = null, // Not yet implemented in entity

                // Payment information (if exists)
                Payment = booking.Payment != null ? new PaymentInfoDto
                {
                    PaymentId = booking.Payment.Id,
                    Amount = booking.Payment.Amount,
                    Status = booking.Payment.Status.ToString(),
                    ProviderRef = booking.Payment.ProviderRef,
                    PaidAt = booking.Payment.PaidAt
                } : null,

                // Payout information (if exists)
                Payout = booking.Payout != null ? new PayoutInfoDto
                {
                    PayoutId = booking.Payout.Id,
                    PlatformShare = booking.Payout.PlatformShare,
                    MotardShare = booking.Payout.MotardShare,
                    Status = booking.Payout.Status.ToString(),
                    SettledAt = booking.Payout.SettledAt
                } : null
            };

            _logger.LogInformation(
                "Returning booking {BookingId} with status {Status}",
                booking.Id,
                booking.Status);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the booking" });
        }
    }

    /// <summary>
    /// Get all bookings (for demo/testing purposes)
    /// </summary>
    /// <returns>List of all bookings with summary information</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetBookings()
    {
        try
        {
            _logger.LogInformation("Retrieving all bookings");

            var bookings = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm!.Motard)
                .Include(b => b.Participant)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var response = bookings.Select(booking => new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? "Unknown",
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard?.Motard?.UserName ?? "Unknown",
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt
            }).ToList();

            _logger.LogInformation("Returning {Count} bookings", response.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings");
            return StatusCode(500, new { error = "An error occurred while retrieving bookings" });
        }
    }
}
