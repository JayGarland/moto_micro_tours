using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Authorization;

/// <summary>
/// Authorization handler for booking access
/// Ensures only the participant, assigned motard, or tour organizer can view booking details
/// </summary>
public class BookingAccessHandler : AuthorizationHandler<BookingAccessRequirement, Booking>
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookingAccessHandler> _logger;

    public BookingAccessHandler(AppDbContext db, ILogger<BookingAccessHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BookingAccessRequirement requirement,
        Booking resource)
    {
        if (resource == null) return;

        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        // Load TourMotard if not already loaded to get motard user ID
        if (resource.TourMotard == null)
        {
            await _db.Entry(resource)
                .Reference(b => b.TourMotard)
                .LoadAsync();
        }

        // Load Tour if not already loaded to get organizer user ID
        if (resource.Tour == null)
        {
            await _db.Entry(resource)
                .Reference(b => b.Tour)
                .LoadAsync();
        }

        var isParticipant = resource.ParticipantUserId == userId;
        var isMotard = resource.TourMotard?.MotardUserId == userId;
        var isOrganizer = resource.Tour?.OrganizerUserId == userId;

        // Allow access if user is the participant, the assigned motard, or the tour organizer
        if (isParticipant || isMotard || isOrganizer)
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "Booking access denied: UserId={UserId} BookingId={BookingId} ParticipantUserId={ParticipantUserId} MotardUserId={MotardUserId} OrganizerUserId={OrganizerUserId}",
            userId,
            resource.Id,
            resource.ParticipantUserId,
            resource.TourMotard?.MotardUserId,
            resource.Tour?.OrganizerUserId);
    }
}
