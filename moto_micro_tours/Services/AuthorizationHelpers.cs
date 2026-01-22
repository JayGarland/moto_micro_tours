using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Services;

/// <summary>
/// Phase 1: Authorization helpers implementation.
/// Provides efficient database queries for checking tour organizer and booking motard permissions.
/// </summary>
public class AuthorizationHelpers : IAuthorizationHelpers
{
    private readonly AppDbContext _context;

    public AuthorizationHelpers(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<bool> IsTourOrganizerAsync(string userId, Guid tourId)
    {
        return await _context.Tours
            .Where(t => t.Id == tourId && t.OrganizerUserId == userId)
            .AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsBookingMotardAsync(string userId, Guid bookingId)
    {
        return await _context.Bookings
            .Where(b => b.Id == bookingId)
            .Include(b => b.TourMotard)
            .Where(b => b.TourMotard != null && b.TourMotard.MotardUserId == userId)
            .AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsTourMotardMemberAsync(string userId, Guid tourId)
    {
        return await _context.TourMotards
            .Where(tm => tm.TourId == tourId && tm.MotardUserId == userId)
            .AnyAsync();
    }
}
