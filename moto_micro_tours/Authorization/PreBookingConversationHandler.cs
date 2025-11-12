using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Authorization;

public class PreBookingConversationHandler : AuthorizationHandler<ConversationAccessRequirement, PreBookingConversation>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ConversationAccessRequirement requirement, PreBookingConversation resource)
    {
        if (resource == null) return Task.CompletedTask;

        // Get user id from claims
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Task.CompletedTask;

        if (resource.MotardUserId == userId || resource.ParticipantUserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
