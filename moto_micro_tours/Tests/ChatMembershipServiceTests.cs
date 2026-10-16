using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;
using moto_micro_tours.Services;
using Xunit;

namespace moto_micro_tours.Tests;

/// <summary>
/// P0 fix: the SignalR hubs must only admit booking participants, assigned
/// motards and pre-booking conversation members into chat groups. These tests
/// pin the membership rules ChatMembershipService enforces for both hubs.
/// </summary>
public class ChatMembershipServiceTests
{
    private (AppDbContext Db, SqliteConnection Connection) GetDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    private static async Task<(Guid bookingId, Guid conversationId)> SeedAsync(AppDbContext db)
    {
        await TestData.SeedUsersAsync(db);
        var tour = TestData.NewTour("Chat Membership Tour");
        var tourMotard = TestData.NewTourMotard(tour.Id, capacity: 2);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TourId = tour.Id,
            TourMotardId = tourMotard.Id,
            TourMotard = tourMotard,
            ParticipantUserId = "participant@test",
            Status = BookingStatus.Approved
        };
        var preConv = new PreBookingConversation
        {
            Id = Guid.NewGuid(),
            TourId = tour.Id,
            MotardUserId = "motard@test",
            ParticipantUserId = "participant@test",
            CreatedAt = DateTime.UtcNow
        };

        db.Tours.Add(tour);
        db.TourMotards.Add(tourMotard);
        db.Bookings.Add(booking);
        db.PreBookingConversations.Add(preConv);
        await db.SaveChangesAsync();
        return (booking.Id, preConv.Id);
    }

    [Fact]
    public async Task BookingGroup_Admits_Participant()
    {
        var (db, connection) = GetDbContext();
        try
        {
            var (bookingId, _) = await SeedAsync(db);
            var service = new ChatMembershipService(db);
            Assert.True(await service.CanJoinBookingGroupAsync("participant@test", bookingId));
        }
        finally { connection.Dispose(); }
    }

    [Fact]
    public async Task BookingGroup_Admits_AssignedMotard()
    {
        var (db, connection) = GetDbContext();
        try
        {
            var (bookingId, _) = await SeedAsync(db);
            var service = new ChatMembershipService(db);
            Assert.True(await service.CanJoinBookingGroupAsync("motard@test", bookingId));
        }
        finally { connection.Dispose(); }
    }

    [Fact]
    public async Task BookingGroup_Denies_UnrelatedUser()
    {
        var (db, connection) = GetDbContext();
        try
        {
            var (bookingId, _) = await SeedAsync(db);
            var service = new ChatMembershipService(db);
            Assert.False(await service.CanJoinBookingGroupAsync("stranger@test", bookingId));
        }
        finally { connection.Dispose(); }
    }

    [Fact]
    public async Task BookingGroup_Denies_UnknownBooking()
    {
        var (db, connection) = GetDbContext();
        try
        {
            var service = new ChatMembershipService(db);
            Assert.False(await service.CanJoinBookingGroupAsync("participant@test", Guid.NewGuid()));
        }
        finally { connection.Dispose(); }
    }

    [Fact]
    public async Task PreBookingGroup_Admits_Members_And_Denies_Others()
    {
        var (db, connection) = GetDbContext();
        try
        {
            var (_, conversationId) = await SeedAsync(db);
            var service = new ChatMembershipService(db);

            Assert.True(await service.CanJoinPreBookingGroupAsync("participant@test", conversationId));
            Assert.True(await service.CanJoinPreBookingGroupAsync("motard@test", conversationId));
            Assert.False(await service.CanJoinPreBookingGroupAsync("stranger@test", conversationId));
            Assert.False(await service.CanJoinPreBookingGroupAsync("participant@test", Guid.NewGuid()));
        }
        finally { connection.Dispose(); }
    }
}
