namespace moto_micro_tours.Services;

/// <summary>
/// Phase 1: Authorization helpers for hierarchical authority model.
/// Provides centralized methods for checking tour organizer and booking motard permissions.
/// </summary>
public interface IAuthorizationHelpers
{
    /// <summary>
    /// Checks if the specified user is the organizer (creator) of the tour.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="tourId">The tour ID.</param>
    /// <returns>True if the user is the tour organizer, false otherwise.</returns>
    Task<bool> IsTourOrganizerAsync(string userId, Guid tourId);

    /// <summary>
    /// Checks if the specified user is the motard assigned to the booking.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="bookingId">The booking ID.</param>
    /// <returns>True if the user is the booking's motard, false otherwise.</returns>
    Task<bool> IsBookingMotardAsync(string userId, Guid bookingId);

    /// <summary>
    /// Checks if the specified user is a motard (TourMotard member) on the tour.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="tourId">The tour ID.</param>
    /// <returns>True if the user is a motard on the tour, false otherwise.</returns>
    Task<bool> IsTourMotardMemberAsync(string userId, Guid tourId);
}
