using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;
using Xunit;

namespace moto_micro_tours.Tests;

/// <summary>
/// P1 fix: Booking.RowVersion and TourMotard.RowVersion are real optimistic
/// concurrency tokens now. A stale write (another context updated the row in
/// between) must throw DbUpdateConcurrencyException instead of silently
/// overwriting — that is what stops two payments taking the same last seat.
/// </summary>
public class RowVersionConcurrencyTests
{
    [Fact]
    public async Task StaleTourMotardUpdate_ThrowsConcurrencyException()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            // Seed one tour motard with the last free seat
            using (var seed = new AppDbContext(options))
            {
                seed.Database.EnsureCreated();
                await TestData.SeedUsersAsync(seed);
                var tour = TestData.NewTour();
                seed.Tours.Add(tour);
                seed.TourMotards.Add(new TourMotard
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    MotardUserId = TestData.MotardId,
                    CapacitySeats = 1,
                    SeatsBooked = 0
                });
                await seed.SaveChangesAsync();
            }

            // Two contexts load the same row (like two concurrent payments)
            await using var ctxA = new AppDbContext(options);
            await using var ctxB = new AppDbContext(options);
            var motardA = await ctxA.TourMotards.SingleAsync();
            var motardB = await ctxB.TourMotards.SingleAsync();

            // Context A takes the seat and commits
            motardA.SeatsBooked += 1;
            await ctxA.SaveChangesAsync();

            // Context B still holds the stale row; its write must not win
            motardB.SeatsBooked += 1;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task StaleBookingUpdate_ThrowsConcurrencyException()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using (var seed = new AppDbContext(options))
            {
                seed.Database.EnsureCreated();
                await TestData.SeedUsersAsync(seed);
                var tour = TestData.NewTour();
                var tourMotard = TestData.NewTourMotard(tour.Id);
                seed.Tours.Add(tour);
                seed.TourMotards.Add(tourMotard);
                seed.Bookings.Add(new Booking
                {
                    Id = Guid.NewGuid(),
                    TourId = tour.Id,
                    TourMotardId = tourMotard.Id,
                    ParticipantUserId = TestData.ParticipantId,
                    Status = BookingStatus.Approved
                });
                await seed.SaveChangesAsync();
            }

            await using var ctxA = new AppDbContext(options);
            await using var ctxB = new AppDbContext(options);
            var bookingA = await ctxA.Bookings.SingleAsync();
            var bookingB = await ctxB.Bookings.SingleAsync();

            bookingA.Status = BookingStatus.Paid;
            await ctxA.SaveChangesAsync();

            bookingB.Status = BookingStatus.Cancelled;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctxB.SaveChangesAsync());
        }
        finally
        {
            connection.Dispose();
        }
    }
}
