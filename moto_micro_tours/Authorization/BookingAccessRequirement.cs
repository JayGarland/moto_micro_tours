using Microsoft.AspNetCore.Authorization;

namespace moto_micro_tours.Authorization;

/// <summary>
/// Authorization requirement for accessing booking details
/// User must be either the participant who created the booking, the assigned motard, or the tour organizer
/// </summary>
public class BookingAccessRequirement : IAuthorizationRequirement
{
}
