using Microsoft.AspNetCore.Authorization;

namespace moto_micro_tours.Authorization;

public class ConversationAccessRequirement : IAuthorizationRequirement
{
    // Marker requirement - handlers enforce membership
}
