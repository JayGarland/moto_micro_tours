using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace moto_micro_tours.Hubs;

[Authorize]
public class BookingChatHub : Hub
{
    /// <summary>
    /// Join a booking's chat group to receive real-time messages
    /// </summary>
    /// <param name="bookingId">The booking ID to join</param>
    public async Task JoinBookingGroup(string bookingId)
    {
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
