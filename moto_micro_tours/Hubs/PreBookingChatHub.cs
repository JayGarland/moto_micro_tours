using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace moto_micro_tours.Hubs;

[Authorize]
public class PreBookingChatHub : Hub
{
    /// <summary>
    /// Join a pre-booking conversation group to receive real-time messages
    /// </summary>
    /// <param name="conversationId">The conversation ID to join</param>
    public async Task JoinPreBookingGroup(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"prebooking-{conversationId}");
    }

    /// <summary>
    /// Leave a pre-booking conversation group
    /// </summary>
    /// <param name="conversationId">The conversation ID to leave</param>
    public async Task LeavePreBookingGroup(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"prebooking-{conversationId}");
    }
}
