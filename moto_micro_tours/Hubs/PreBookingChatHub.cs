using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using moto_micro_tours.Services;

namespace moto_micro_tours.Hubs;

[Authorize]
public class PreBookingChatHub : Hub
{
    private readonly IChatMembershipService _membership;

    public PreBookingChatHub(IChatMembershipService membership)
    {
        _membership = membership;
    }

    /// <summary>
    /// Join a pre-booking conversation group to receive real-time messages.
    /// Only the conversation's motard and participant may join.
    /// </summary>
    /// <param name="conversationId">The conversation ID to join</param>
    public async Task JoinPreBookingGroup(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var id)
            || !await _membership.CanJoinPreBookingGroupAsync(GetUserId(), id))
        {
            throw new HubException("Not allowed to join this conversation chat");
        }

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

    /// <summary>
    /// Get the current user's ID from claims
    /// </summary>
    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
