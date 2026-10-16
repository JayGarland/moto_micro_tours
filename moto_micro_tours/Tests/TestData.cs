using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Tests;

/// <summary>
/// Shared helpers for unit tests: FK-valid users, tour, motard and booking
/// rows (Tours.OrganizerUserId, Booking.ParticipantUserId and TourMotard
/// foreign keys are all required by the model).
/// </summary>
public static class TestData
{
    public const string OrganizerId = "organizer@test";
    public const string MotardId = "motard@test";
    public const string ParticipantId = "participant@test";
    public const string StrangerId = "stranger@test";

    public static async Task SeedUsersAsync(AppDbContext db)
    {
        foreach (var id in new[] { OrganizerId, MotardId, ParticipantId, StrangerId })
        {
            if (!await db.Users.AnyAsync(u => u.Id == id))
            {
                db.Users.Add(new AppUser
                {
                    Id = id,
                    UserName = id,
                    NormalizedUserName = id.ToUpperInvariant(),
                    Email = id,
                    NormalizedEmail = id.ToUpperInvariant(),
                    EmailConfirmed = true
                });
            }
        }
        await db.SaveChangesAsync();
    }

    public static Tour NewTour(string title = "Test Tour", TourStatus status = TourStatus.Published) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            StartAt = DateTime.UtcNow,
            StartLocationText = "A",
            EndLocationText = "B",
            Status = status,
            OrganizerUserId = OrganizerId
        };

    public static TourMotard NewTourMotard(Guid tourId, int capacity = 5, int seatsBooked = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            MotardUserId = MotardId,
            CapacitySeats = capacity,
            SeatsBooked = seatsBooked
        };

    public static Booking NewBooking(Guid tourId, Guid tourMotardId, BookingStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            TourMotardId = tourMotardId,
            ParticipantUserId = ParticipantId,
            Status = status
        };
}
