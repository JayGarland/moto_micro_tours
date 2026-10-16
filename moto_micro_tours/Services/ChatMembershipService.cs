using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Services;

/// <summary>
/// Membership checks for chat groups, shared by the HTTP chat endpoints
/// (via authorization handlers) and the SignalR hubs (directly).
/// The rule is the same everywhere: only the booking's participant and its
/// assigned motard may join a booking chat; only a pre-booking conversation's
/// motard and participant may join that conversation's chat.
/// </summary>
public interface IChatMembershipService
{
    Task<bool> CanJoinBookingGroupAsync(string? userId, Guid bookingId);
    Task<bool> CanJoinPreBookingGroupAsync(string? userId, Guid conversationId);
}

public class ChatMembershipService : IChatMembershipService
{
    private readonly AppDbContext _db;

    public ChatMembershipService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<bool> CanJoinBookingGroupAsync(string? userId, Guid bookingId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        return await _db.Bookings
            .Include(b => b.TourMotard)
            .Where(b => b.Id == bookingId)
            .Where(b => b.ParticipantUserId == userId
                     || (b.TourMotard != null && b.TourMotard.MotardUserId == userId))
            .AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CanJoinPreBookingGroupAsync(string? userId, Guid conversationId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        return await _db.PreBookingConversations
            .Where(c => c.Id == conversationId)
            .Where(c => c.MotardUserId == userId || c.ParticipantUserId == userId)
            .AnyAsync();
    }
}
