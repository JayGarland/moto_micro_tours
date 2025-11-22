using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Authorization;

/// <summary>
/// Authorization handler for booking access
/// Ensures only the participant or assigned motard can view booking details
/// </summary>
public class BookingAccessHandler : AuthorizationHandler<BookingAccessRequirement, Booking>
{
    private readonly AppDbContext _db;

    public BookingAccessHandler(AppDbContext db)
    {
        _db = db;
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

        // Allow access if user is the participant OR the assigned motard
        if (resource.ParticipantUserId == userId || resource.TourMotard?.MotardUserId == userId)
        {
            context.Succeed(requirement);
        }
    }
}
