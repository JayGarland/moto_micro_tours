using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using moto_micro_tours.Services;

namespace moto_micro_tours.Hubs;

[Authorize]
public class BookingChatHub : Hub
{
    private readonly IChatMembershipService _membership;

    public BookingChatHub(IChatMembershipService membership)
    {
        _membership = membership;
    }

    /// <summary>
    /// Join a booking's chat group to receive real-time messages.
    /// Only the booking's participant and its assigned motard may join.
    /// </summary>
    /// <param name="bookingId">The booking ID to join</param>
    public async Task JoinBookingGroup(string bookingId)
    {
        if (!Guid.TryParse(bookingId, out var id)
            || !await _membership.CanJoinBookingGroupAsync(GetUserId(), id))
        {
            throw new HubException("Not allowed to join this booking chat");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
    }

    /// <summary>
    /// Leave a booking's chat group
    /// </summary>
    /// <param name="bookingId">The booking ID to leave</param>
    public async Task LeaveBookingGroup(string bookingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"booking-{bookingId}");
    }

    /// <summary>
    /// Get the current user's ID from claims
    /// </summary>
    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
