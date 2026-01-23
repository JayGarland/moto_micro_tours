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
    private readonly IAuthorizationService _authorizationService;
    private readonly moto_micro_tours.Services.IAuthorizationHelpers _authHelpers; // Phase 1

    public BookingsController(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger<BookingsController> logger,
        IAuthorizationService authorizationService,
        moto_micro_tours.Services.IAuthorizationHelpers authHelpers) // Phase 1
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _authorizationService = authorizationService;
        _authHelpers = authHelpers; // Phase 1
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
                MessageToMotard = string.IsNullOrWhiteSpace(request.MessageToMotard)
                    ? null
                    : request.MessageToMotard.Trim(),
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
                ParticipantName = currentUser.UserName ?? "Unknown",
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                TourStartAt = tour.StartAt,
                PricePerSeat = tourMotard.PricePerSeat,
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc,
                ApprovedByUserId = booking.ApprovedByUserId
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
    /// Approve a booking request (Stage F: Only the assigned motard or tour organizer can approve)
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

            var isBookingMotard = booking.TourMotard.MotardUserId == currentUser.Id;
            var isTourOrganizer = booking.Tour?.OrganizerUserId == currentUser.Id;

            // Only the assigned motard or tour organizer can approve
            if (!isBookingMotard && !isTourOrganizer)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to approve booking {BookingId} but is neither the booking motard nor the tour organizer",
                    currentUser.Id, id);
                return Forbid(); // 403 Forbidden - user is authenticated but not authorized
            }

            LogOrganizerAccessIfNotParty(nameof(ApproveBooking), booking, currentUser.Id);

            var approverRole = isTourOrganizer ? "tour organizer" : "booking motard";
            _logger.LogInformation(
                "{Role} {UserId} ({Username}) is authorized to approve booking {BookingId}",
                approverRole, currentUser.Id, currentUser.UserName, id);

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
                ParticipantName = booking.Participant?.UserName ?? "Unknown",
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc,
                ApprovedByUserId = booking.ApprovedByUserId
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
    /// Decline a booking request (Stage F7.1: Only the assigned motard or tour organizer can decline)
    /// Sets status to Declined and optionally stores a decline reason
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Optional decline reason</param>
    /// <returns>Updated booking with Declined status</returns>
    [HttpPost("{id}/decline")]
    [Authorize(Roles = "Motard")] // Must be a Motard, and specifically the one assigned to this booking
    public async Task<ActionResult<BookingResponseDto>> DeclineBooking(Guid id, [FromBody] DeclineBookingRequestDto? request)
    {
        try
        {
            _logger.LogInformation("Declining booking {BookingId}", id);

            // Load booking with related entities
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm!.Motard)
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
                    "Booking {BookingId} cannot be declined (current status: {Status})",
                    id, booking.Status);
                return BadRequest(new
                {
                    error = $"Booking cannot be declined. Current status: {booking.Status}"
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
                    error = $"Tour is no longer available (status: {booking.Tour?.Status})"
                });
            }

            // Stage F7.1: Verify current user is the assigned motard
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Decline attempt without authentication for booking {BookingId}", id);
                return Unauthorized(new { error = "You must be logged in to decline bookings" });
            }

            var isBookingMotard = booking.TourMotard?.MotardUserId == currentUser.Id;
            var isTourOrganizer = booking.Tour?.OrganizerUserId == currentUser.Id;

            // Only the assigned motard or tour organizer can decline
            if (!isBookingMotard && !isTourOrganizer)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to decline booking {BookingId} but is neither the booking motard nor the tour organizer",
                    currentUser.Id, id);
                return Forbid(); // 403 Forbidden - user is authenticated but not authorized
            }

            LogOrganizerAccessIfNotParty(nameof(DeclineBooking), booking, currentUser.Id);

            var declinerRole = isTourOrganizer ? "tour organizer" : "booking motard";
            _logger.LogInformation(
                "{Role} {UserId} ({Username}) is authorized to decline booking {BookingId}",
                declinerRole, currentUser.Id, currentUser.UserName, id);

            // Decline the booking
            booking.Status = BookingStatus.Declined;
            booking.DeclinedAt = DateTime.UtcNow;
            booking.DeclineReason = request?.Reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking {BookingId} declined{ReasonSuffix}",
                booking.Id,
                string.IsNullOrWhiteSpace(request?.Reason) ? "" : $" with reason: {request.Reason}");

            var response = new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? "Unknown",
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard?.Motard?.UserName ?? "Unknown",
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                ParticipantName = booking.Participant?.UserName ?? "Unknown",
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                DeclinedAt = booking.DeclinedAt,
                DeclineReason = booking.DeclineReason,
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc,
                ApprovedByUserId = booking.ApprovedByUserId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while declining the booking" });
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

            // Authorization: Only participant, assigned motard, or tour organizer can view booking details
            var authResult = await _authorizationService.AuthorizeAsync(User, booking, "CanAccessBookingDetails");
            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access booking {BookingId} without authorization",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    id);
                // Return 404 instead of 403 to avoid leaking booking existence
                return NotFound(new { error = $"Booking with ID {id} not found" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                LogOrganizerAccessIfNotParty(nameof(GetBookingById), booking, currentUser.Id);
            }

            // Lazy update: Check if booking has expired and update status
            if (booking.Status == BookingStatus.Approved &&
                booking.ExpiresAt.HasValue &&
                DateTime.UtcNow > booking.ExpiresAt.Value)
            {
                _logger.LogInformation(
                    "Booking {BookingId} has expired (ExpiresAt: {ExpiresAt}). Updating status to Cancelled.",
                    booking.Id, booking.ExpiresAt);

                booking.Status = BookingStatus.Cancelled; //OPTIMIZE: BookingStatus.CancelledExpired hasn't been used and should have been used here
                booking.CancelledAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
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
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                PaidAt = booking.PaidAt,
                CancelledAt = booking.CancelledAt,
                CancellationReason = null, // Not yet implemented in entity
                DeclinedAt = booking.DeclinedAt, // Stage F7.1
                DeclineReason = booking.DeclineReason, // Stage F7.1

                // Completion timestamps (F8 - Booking Completion Handshake)
                CompletedByParticipantAtUtc = booking.CompletedByParticipantAtUtc,
                CompletedByMotardAtUtc = booking.CompletedByMotardAtUtc,
                CompletedAtUtc = booking.CompletedAtUtc,

                // Phase 1: Start time override audit
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc,
                ApprovedByUserId = booking.ApprovedByUserId,

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
    /// Get bookings for the current authenticated user
    /// Returns:
    /// - For Participants: Bookings they created (as participant)
    /// - For Motards: Bookings for tours they're assigned to (as motard)
    /// </summary>
    /// <returns>List of user's bookings with summary information</returns>
    [HttpGet]
    [Authorize] // Must be authenticated
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetBookings()
    {
        try
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "You must be logged in to view bookings" });
            }

            var roles = await _userManager.GetRolesAsync(currentUser);
            var isMotard = roles.Contains("Motard");
            var isOrganizer = await _context.Tours.AnyAsync(t => t.OrganizerUserId == currentUser.Id);

            _logger.LogInformation(
                "Retrieving bookings for user {UserId} (isMotard: {IsMotard}, isOrganizer: {IsOrganizer})",
                currentUser.Id, isMotard, isOrganizer);

            IQueryable<Booking> query = _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm!.Motard)
                .Include(b => b.Participant);

            // Filter based on user role
            if (isMotard || isOrganizer)
            {
                // Motards see bookings for tours they're assigned to, organizers see bookings for tours they run
                query = query.Where(b =>
                    (isMotard && b.TourMotard!.MotardUserId == currentUser.Id) ||
                    (isOrganizer && b.Tour != null && b.Tour.OrganizerUserId == currentUser.Id));
            }
            else
            {
                // Participants see their own bookings
                query = query.Where(b => b.ParticipantUserId == currentUser.Id);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            if (isOrganizer)
            {
                foreach (var booking in bookings.Where(b => b.Tour?.OrganizerUserId == currentUser.Id))
                {
                    LogOrganizerAccessIfNotParty(nameof(GetBookings), booking, currentUser.Id);
                }
            }

            // Lazy update: Check for expired bookings and update their status
            var expiredBookings = bookings.Where(b =>
                b.Status == BookingStatus.Approved &&
                b.ExpiresAt.HasValue &&
                DateTime.UtcNow > b.ExpiresAt.Value).ToList();

            if (expiredBookings.Any())
            {
                foreach (var expiredBooking in expiredBookings)
                {
                    _logger.LogInformation(
                        "Booking {BookingId} has expired (ExpiresAt: {ExpiresAt}). Updating status to Cancelled.",
                        expiredBooking.Id, expiredBooking.ExpiresAt);

                    expiredBooking.Status = BookingStatus.Cancelled;
                    expiredBooking.CancelledAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }

            var response = bookings.Select(booking => new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? "Unknown",
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard?.Motard?.UserName ?? "Unknown",
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                ParticipantName = booking.Participant?.UserName ?? "Unknown",
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                DeclinedAt = booking.DeclinedAt, // Stage F7.1
                DeclineReason = booking.DeclineReason, // Stage F7.1
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,
                CompletedByParticipantAtUtc = booking.CompletedByParticipantAtUtc, // B1
                CompletedByMotardAtUtc = booking.CompletedByMotardAtUtc, // B1
                CompletedAtUtc = booking.CompletedAtUtc, // B1
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc, // Phase 1
                ApprovedStartReason = booking.ApprovedStartReason, // Phase 1
                ApprovedAtUtc = booking.ApprovedAtUtc, // Phase 1
                ApprovedByUserId = booking.ApprovedByUserId // Phase 1
            }).ToList();

            _logger.LogInformation(
                "Returning {Count} bookings for user {UserId}",
                response.Count, currentUser.Id);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings");
            return StatusCode(500, new { error = "An error occurred while retrieving bookings" });
        }
    }

    /// <summary>
    /// Cancel a booking (Stage G: Participant only)
    /// Rules:
    /// - Before payment (Pending/Approved): Free cancellation
    /// - After payment + >24h before tour: Full refund
    /// - After payment + <24h before tour: Must request from motard (returns 400)
    /// - Cancellation increases motard's available capacity
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Updated booking with Cancelled status</returns>
    [HttpPost("{id}/cancel")]
    [Authorize] // Must be authenticated
    public async Task<ActionResult<BookingResponseDto>> CancelBooking(Guid id)
    {
        try
        {
            _logger.LogInformation("Cancelling booking {BookingId}", id);

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "You must be logged in to cancel bookings" });
            }

            // Load booking with related entities
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm!.Motard)
                .Include(b => b.Participant)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", id);
                return NotFound(new { error = $"Booking with ID {id} not found" });
            }

            // Lazy update: Check if booking has expired and update status
            if (booking.Status == BookingStatus.Approved &&
                booking.ExpiresAt.HasValue &&
                DateTime.UtcNow > booking.ExpiresAt.Value)
            {
                _logger.LogInformation(
                    "Booking {BookingId} has expired (ExpiresAt: {ExpiresAt}). Updating status to Cancelled.",
                    booking.Id, booking.ExpiresAt);

                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Rule: Only the participant can cancel their booking
            if (booking.ParticipantUserId != currentUser.Id)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to cancel booking {BookingId} owned by {ParticipantUserId}",
                    currentUser.Id, id, booking.ParticipantUserId);
                return Forbid(); // 403 Forbidden
            }

            // Check if booking can be cancelled
            if (booking.Status == BookingStatus.Cancelled)
            {
                return BadRequest(new { error = "Booking is already cancelled" });
            }

            if (booking.Status == BookingStatus.Refunded)
            {
                return BadRequest(new { error = "Booking is already refunded" });
            }

            // Rule: If booking is Paid, check if >24h before tour
            bool needsRefund = false;
            if (booking.Status == BookingStatus.Paid)
            {
                if (booking.Tour == null)
                {
                    return BadRequest(new { error = "Tour information not found" });
                }

                var hoursUntilTour = (booking.Tour.StartAt - DateTime.UtcNow).TotalHours;

                if (hoursUntilTour < 24)
                {
                    _logger.LogWarning(
                        "Booking {BookingId} cancellation denied: less than 24h before tour (hours: {Hours})",
                        id, hoursUntilTour);
                    return BadRequest(new
                    {
                        error = "Cannot cancel within 24 hours of tour start. Please contact the motard directly.",
                        hoursUntilTour = Math.Round(hoursUntilTour, 1)
                    });
                }

                needsRefund = true;
                _logger.LogInformation(
                    "Booking {BookingId} eligible for full refund (hours until tour: {Hours})",
                    id, hoursUntilTour);
            }

            // Cancel the booking
            var wasPaid = booking.Status == BookingStatus.Paid;
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;

            // Only free capacity if booking was Paid (seats were actually reserved)
            // Pending/Approved bookings don't reserve seats yet
            if (wasPaid && booking.TourMotard != null)
            {
                booking.TourMotard.SeatsBooked -= 1;
                _logger.LogInformation(
                    "TourMotard {TourMotardId} capacity freed (was paid): {SeatsBooked}/{CapacitySeats}",
                    booking.TourMotardId,
                    booking.TourMotard.SeatsBooked,
                    booking.TourMotard.CapacitySeats);
            }
            else
            {
                _logger.LogInformation(
                    "TourMotard {TourMotardId} capacity not freed (booking was {Status}, not Paid)",
                    booking.TourMotardId,
                    booking.Status);
            }

            // Mock refund if needed
            if (needsRefund && booking.Payment != null)
            {
                booking.Payment.Status = PaymentStatus.Refunded;
                // Note: Payment entity doesn't have RefundedAt field - using PaidAt to track refund time
                _logger.LogInformation(
                    "Payment {PaymentId} marked as refunded (mock)",
                    booking.Payment.Id);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking {BookingId} cancelled successfully by participant {UserId}",
                booking.Id, currentUser.Id);

            var response = new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? "Unknown",
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard?.Motard?.UserName ?? "Unknown",
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                ParticipantName = booking.Participant?.UserName ?? "Unknown",
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc,
                ApprovedByUserId = booking.ApprovedByUserId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while cancelling the booking" });
        }
    }

    /// <summary>
    /// Refund a paid booking (Stage G: Motard or tour organizer)
    /// Rules:
    /// - Only the assigned motard or tour organizer can refund
    /// - Booking must be in Paid status
    /// - Full refund (mock) is processed
    /// - Booking status changes to Refunded (not Cancelled)
    /// - Payout status changes to Failed
    /// - Capacity is freed up
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Updated booking with Refunded status</returns>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = "Motard")] // Must be a Motard
    public async Task<ActionResult<BookingResponseDto>> RefundBooking(Guid id)
    {
        try
        {
            _logger.LogInformation("Processing refund for booking {BookingId}", id);

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "You must be logged in to refund bookings" });
            }

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

            // Lazy update: Check if booking has expired and update status
            if (booking.Status == BookingStatus.Approved &&
                booking.ExpiresAt.HasValue &&
                DateTime.UtcNow > booking.ExpiresAt.Value)
            {
                _logger.LogInformation(
                    "Booking {BookingId} has expired (ExpiresAt: {ExpiresAt}). Updating status to Cancelled.",
                    booking.Id, booking.ExpiresAt);

                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var isBookingMotard = booking.TourMotard?.MotardUserId == currentUser.Id;
            var isTourOrganizer = booking.Tour?.OrganizerUserId == currentUser.Id;

            // Rule: Only the assigned motard or tour organizer can refund this booking
            if (!isBookingMotard && !isTourOrganizer)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to refund booking {BookingId} but is neither the booking motard nor the tour organizer",
                    currentUser.Id, id);
                return Forbid(); // 403 Forbidden
            }

            LogOrganizerAccessIfNotParty(nameof(RefundBooking), booking, currentUser.Id);
            var refundRole = isTourOrganizer ? "tour organizer" : "booking motard";

            // Rule: Can only refund Paid bookings
            if (booking.Status != BookingStatus.Paid)
            {
                _logger.LogWarning(
                    "Booking {BookingId} cannot be refunded (current status: {Status})",
                    id, booking.Status);
                return BadRequest(new
                {
                    error = $"Only paid bookings can be refunded. Current status: {booking.Status}"
                });
            }

            // Check if already refunded
            if (booking.Status == BookingStatus.Refunded)
            {
                return BadRequest(new { error = "Booking is already refunded" });
            }

            // Process refund
            booking.Status = BookingStatus.Refunded;
            booking.CancelledAt = DateTime.UtcNow;

            // Mark payment as refunded (mock)
            if (booking.Payment != null)
            {
                booking.Payment.Status = PaymentStatus.Refunded;
                _logger.LogInformation(
                    "Payment {PaymentId} marked as refunded (mock)",
                    booking.Payment.Id);
            }

            // Update payout status to Failed
            if (booking.Payout != null)
            {
                booking.Payout.Status = PayoutStatus.Failed;
                _logger.LogInformation(
                    "Payout {PayoutId} status changed to Failed",
                    booking.Payout.Id);
            }

            // Free up capacity
            if (booking.TourMotard != null)
            {
                booking.TourMotard.SeatsBooked -= 1;
                _logger.LogInformation(
                    "TourMotard {TourMotardId} capacity freed: {SeatsBooked}/{CapacitySeats}",
                    booking.TourMotardId,
                    booking.TourMotard.SeatsBooked,
                    booking.TourMotard.CapacitySeats);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking {BookingId} refunded successfully by {Role} {UserId}",
                booking.Id, refundRole, currentUser.Id);

            var response = new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? "Unknown",
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard?.Motard?.UserName ?? "Unknown",
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                ParticipantName = booking.Participant?.UserName ?? "Unknown",
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc,
                ApprovedByUserId = booking.ApprovedByUserId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while refunding the booking" });
        }
    }

    /// <summary>
    /// Get the conversation ID for a booking (F7.2 Phase 1 helper)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Conversation ID if exists, null otherwise</returns>
    [HttpGet("{id}/conversation")]
    [Authorize]
    public async Task<ActionResult<BookingConversationDto>> GetBookingConversation(Guid id)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Load booking with related data
            var booking = await _context.Bookings
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm.Motard)
                .Include(b => b.Participant)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { error = "Booking not found" });
            }

            // Check authorization: user must be motard or participant
            var isMotard = booking.TourMotard?.MotardUserId == currentUser.Id;
            var isParticipant = booking.ParticipantUserId == currentUser.Id;

            if (!isMotard && !isParticipant)
            {
                return Forbid();
            }

            // Check if booking status allows chat (Approved, Paid, Cancelled, CancelledExpired, Refunded)
            if (booking.Status != BookingStatus.Approved &&
                booking.Status != BookingStatus.Paid &&
                booking.Status != BookingStatus.Cancelled &&
                booking.Status != BookingStatus.CancelledExpired &&
                booking.Status != BookingStatus.Refunded)
            {
                return BadRequest(new { error = "Chat is only available for approved or completed bookings" });
            }

            // Find existing conversation
            var conversation = await _context.BookingConversations
                .FirstOrDefaultAsync(c => c.BookingId == id);

            if (conversation == null)
            {
                return Ok(new BookingConversationDto
                {
                    BookingId = id,
                    ConversationId = null,
                    MotardUserId = booking.TourMotard?.MotardUserId,
                    MotardName = booking.TourMotard?.Motard?.UserName,
                    ParticipantUserId = booking.ParticipantUserId,
                    ParticipantName = booking.Participant?.UserName
                });
            }

            return Ok(new BookingConversationDto
            {
                BookingId = id,
                ConversationId = conversation.Id,
                MotardUserId = booking.TourMotard?.MotardUserId,
                MotardName = booking.TourMotard?.Motard?.UserName,
                ParticipantUserId = booking.ParticipantUserId,
                ParticipantName = booking.Participant?.UserName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation for booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the conversation" });
        }
    }

    /// <summary>
    /// Phase 1: Approve effective start time for booking completion (formerly Option E)
    /// POST /api/bookings/{id}/approve-start-time
    /// - Allows booking's motard OR tour organizer to set earlier effective start time
    /// - Enables completion before Tour.StartAt if needed
    /// - Audit trail via ApprovedAtUtc and ApprovedByUserId
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Approved start time and optional reason</param>
    /// <returns>Updated booking</returns>
    [HttpPost("{id}/approve-start-time")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> ApproveStartTime(
        Guid id,
        [FromBody] ApproveStartTimeRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Approving start time for booking {BookingId} to {ApprovedTime}",
                id, request.ApprovedStartAtUtc);

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "You must be logged in" });
            }

            // Load booking with related entities
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", id);
                return NotFound(new { error = $"Booking with ID {id} not found" });
            }

            // Phase 1: Verify user is EITHER booking's motard OR tour organizer
            bool isBookingMotard = await _authHelpers.IsBookingMotardAsync(currentUser.Id, id);
            bool isTourOrganizer = booking.Tour != null &&
                await _authHelpers.IsTourOrganizerAsync(currentUser.Id, booking.Tour.Id);

            if (!isBookingMotard && !isTourOrganizer)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to approve start time for booking {BookingId} " +
                    "but is neither the booking's motard nor the tour organizer",
                    currentUser.Id, id);
                return Forbid();
            }

            string approverRole = isTourOrganizer ? "tour organizer" : "booking motard";
            LogOrganizerAccessIfNotParty(nameof(ApproveStartTime), booking, currentUser.Id);
            _logger.LogInformation(
                "User {UserId} authorized as {Role} to approve start time for booking {BookingId}",
                currentUser.Id, approverRole, id);

            // Validate booking status (prefer Paid)
            if (booking.Status != BookingStatus.Paid)
            {
                _logger.LogWarning(
                    "Cannot approve start time for booking {BookingId} with status {Status}",
                    id, booking.Status);
                return BadRequest(new
                {
                    error = $"Can only approve start time for paid bookings. Current status: {booking.Status}"
                });
            }

            // Validate approved time is reasonable (not too far in future)
            if (request.ApprovedStartAtUtc > DateTime.UtcNow.AddDays(365))
            {
                return BadRequest(new { error = "Approved start time cannot be more than 1 year in the future" });
            }

            // Set approval fields (Phase 1: temporary, will update in Task 9)
            booking.ApprovedStartAtUtc = request.ApprovedStartAtUtc;
            booking.ApprovedStartReason = request.Reason;
            booking.ApprovedAtUtc = DateTime.UtcNow;
            booking.ApprovedByUserId = currentUser.Id;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking {BookingId} start time approved by {Role} {UserId}. Effective start: {EffectiveStart}",
                id, approverRole, currentUser.Id, request.ApprovedStartAtUtc);

            return Ok(new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? string.Empty,
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard?.Motard?.UserName ?? string.Empty,
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                ParticipantName = booking.Participant?.UserName ?? string.Empty,
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc, // Phase 1
                ApprovedByUserId = booking.ApprovedByUserId // Phase 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving start time for booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while approving start time" });
        }
    }

    /// <summary>
    /// F8: Mark booking as completed by participant or motard
    /// POST /api/bookings/{id}/complete
    /// - Sets CompletedByParticipantAtUtc or CompletedByMotardAtUtc based on caller
    /// - When both are set, sets CompletedAtUtc
    /// - Idempotent: calling twice doesn't break
    /// - Phase 1: Uses EffectiveStartAtUtc (ApprovedStartAtUtc ?? Tour.StartAt) for gate
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Updated booking with completion timestamps</returns>
    [HttpPost("{id}/complete")]
    [Authorize]
    public async Task<ActionResult<BookingResponseDto>> CompleteBooking(Guid id)
    {
        try
        {
            _logger.LogInformation("Marking booking {BookingId} as completed", id);

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { error = "You must be logged in to complete bookings" });
            }

            // Load booking with related entities
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                .Include(b => b.Participant)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", id);
                return NotFound(new { error = $"Booking with ID {id} not found" });
            }

            // Verify user is participant or motard
            bool isParticipant = booking.ParticipantUserId == currentUser.Id;
            bool isMotard = booking.TourMotard?.MotardUserId == currentUser.Id;

            if (!isParticipant && !isMotard)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to complete booking {BookingId} they're not part of",
                    currentUser.Id, id);
                return Forbid();
            }

            // Validate booking must be Paid
            if (booking.Status != BookingStatus.Paid)
            {
                _logger.LogWarning(
                    "Cannot complete booking {BookingId} with status {Status}",
                    id, booking.Status);
                return BadRequest(new
                {
                    error = $"Can only complete paid bookings. Current status: {booking.Status}"
                });
            }

            // Phase 1: Calculate effective start time
            // EffectiveStartAtUtc = ApprovedStartAtUtc ?? Tour.StartAt
            if (booking.Tour == null)
            {
                return BadRequest(new { error = "Tour information not found" });
            }

            var effectiveStartAtUtc = booking.ApprovedStartAtUtc ?? booking.Tour.StartAt;

            // Validate effective start time has passed
            if (effectiveStartAtUtc > DateTime.UtcNow)
            {
                var startType = booking.ApprovedStartAtUtc.HasValue ? "approved" : "scheduled";
                _logger.LogInformation(
                    "Cannot complete booking {BookingId} - {StartType} start time hasn't passed yet (starts: {StartAt}). Using EffectiveStartAtUtc.",
                    id, startType, effectiveStartAtUtc);
                return BadRequest(new
                {
                    error = $"Cannot complete booking before {startType} start time",
                    effectiveStartAtUtc = effectiveStartAtUtc,
                    tourStartAt = booking.Tour.StartAt,
                    approvedStartAtUtc = booking.ApprovedStartAtUtc
                });
            }

            // Set completion timestamp (idempotent)
            var now = DateTime.UtcNow;
            bool timestampUpdated = false;

            if (isParticipant && !booking.CompletedByParticipantAtUtc.HasValue)
            {
                booking.CompletedByParticipantAtUtc = now;
                timestampUpdated = true;
                _logger.LogInformation(
                    "Booking {BookingId} marked as completed by participant {UserId}",
                    id, currentUser.Id);
            }
            else if (isMotard && !booking.CompletedByMotardAtUtc.HasValue)
            {
                booking.CompletedByMotardAtUtc = now;
                timestampUpdated = true;
                _logger.LogInformation(
                    "Booking {BookingId} marked as completed by motard {UserId}",
                    id, currentUser.Id);
            }
            else
            {
                // Already completed by this user - idempotent, no error
                _logger.LogInformation(
                    "Booking {BookingId} already marked as completed by {Role}",
                    id, isParticipant ? "participant" : "motard");
            }

            // Save individual timestamp first
            if (timestampUpdated)
            {
                await _context.SaveChangesAsync();

                // Reload to get latest state (race safety: other party may have just completed)
                await _context.Entry(booking).ReloadAsync();
            }

            // Check if both sides have now completed and set CompletedAtUtc
            if (booking.CompletedByParticipantAtUtc.HasValue &&
                booking.CompletedByMotardAtUtc.HasValue &&
                !booking.CompletedAtUtc.HasValue)
            {
                booking.CompletedAtUtc = DateTime.UtcNow; // Use fresh timestamp
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Booking {BookingId} fully completed by both parties",
                    id);
            }

            // Return booking response
            return Ok(new BookingResponseDto
            {
                BookingId = booking.Id,
                TourId = booking.TourId,
                TourTitle = booking.Tour?.Title ?? string.Empty,
                TourMotardId = booking.TourMotardId,
                MotardName = booking.TourMotard?.Motard?.UserName ?? string.Empty,
                ParticipantId = Guid.Parse(booking.ParticipantUserId),
                ParticipantName = booking.Participant?.UserName ?? string.Empty,
                Status = booking.Status.ToString(),
                CostSharingOptIn = booking.CostSharingOptIn,
                EmergencyContact = booking.EmergencyContact ?? string.Empty,
                MessageToMotard = booking.MessageToMotard,
                CreatedAt = booking.CreatedAt,
                ApprovedAt = booking.ApprovedAt,
                ExpiresAt = booking.ExpiresAt,
                DeclinedAt = booking.DeclinedAt,
                DeclineReason = booking.DeclineReason,
                TourStartAt = booking.Tour?.StartAt ?? DateTime.MinValue,
                PricePerSeat = booking.TourMotard?.PricePerSeat ?? 0,
                CompletedByParticipantAtUtc = booking.CompletedByParticipantAtUtc,
                CompletedByMotardAtUtc = booking.CompletedByMotardAtUtc,
                CompletedAtUtc = booking.CompletedAtUtc,
                ApprovedStartAtUtc = booking.ApprovedStartAtUtc,
                ApprovedStartReason = booking.ApprovedStartReason,
                ApprovedAtUtc = booking.ApprovedAtUtc,
                ApprovedByUserId = booking.ApprovedByUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing booking {BookingId}", id);
            return StatusCode(500, new { error = "An error occurred while completing the booking" });
        }
    }

    private static bool IsBookingParty(Booking booking, string userId)
    {
        return booking.ParticipantUserId == userId ||
               booking.TourMotard?.MotardUserId == userId;
    }

    private static bool IsTourOrganizer(Booking booking, string userId)
    {
        return booking.Tour?.OrganizerUserId == userId;
    }

    private void LogOrganizerAccessIfNotParty(string actionName, Booking booking, string organizerUserId)
    {
        if (IsTourOrganizer(booking, organizerUserId) && !IsBookingParty(booking, organizerUserId))
        {
            _logger.LogInformation(
                "Organizer booking access: Action={Action} BookingId={BookingId} TourId={TourId} OrganizerUserId={OrganizerUserId}",
                actionName, booking.Id, booking.TourId, organizerUserId);
        }
    }


    // TODO: Implement logic to allow refunded bookings to leave reviews.
    // TODO: Add logic to block new bookings and prevent payments for tours with status Cancelled.
    // TODO: in a far future, not scheduled, Implement logic to enforce rate limits using the RateLimitCounter entity.
}
