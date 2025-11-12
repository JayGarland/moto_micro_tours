using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Authorization;

public class BookingConversationHandler : AuthorizationHandler<ConversationAccessRequirement, BookingConversation>
{
    private readonly AppDbContext _db;

    public BookingConversationHandler(AppDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ConversationAccessRequirement requirement, BookingConversation resource)
    {
        if (resource == null) return;

        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        // Load booking with tour motard to determine motard user id
        var booking = await _db.Bookings
            .Include(b => b.TourMotard)
            .FirstOrDefaultAsync(b => b.Id == resource.BookingId);

        if (booking == null) return;

        if (booking.ParticipantUserId == userId || booking.TourMotard?.MotardUserId == userId)
        {
            context.Succeed(requirement);
        }
    }
}
