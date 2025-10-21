using moto_micro_tours;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Xunit;
using moto_micro_tours.Domain.Enums;
using System;
using System.Threading.Tasks;

public class PaymentServiceTests
{
    private AppDbContext GetDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        // Create schema
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldSucceed_ForValidBooking()
    {
        var db = GetDbContext();
        // Seed booking, tourmotard, etc. (simplified)
    var tourId = Guid.NewGuid();
    var tour = new Tour { Id = tourId, Title = "Test Tour", StartAt = DateTime.UtcNow, StartLocationText = "A", EndLocationText = "B", Status = TourStatus.Published };
    var tourMotard = new TourMotard { Id = Guid.NewGuid(), TourId = tourId, MotardUserId = "motard@test", CapacitySeats = 5, SeatsBooked = 0, RowVersion = DateTime.UtcNow };
    var booking = new Booking { Id = Guid.NewGuid(), TourId = tourId, TourMotardId = tourMotard.Id, TourMotard = tourMotard, ParticipantUserId = "participant@test", Status = BookingStatus.Approved, RowVersion = DateTime.UtcNow };
    db.Tours.Add(tour);
    db.TourMotards.Add(tourMotard);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var service = new PaymentService(db);
        var result = await service.ProcessPaymentAsync(booking.Id, 100m);
        Assert.True(result);
        Assert.Equal(1, tourMotard.SeatsBooked);
    Assert.Equal(BookingStatus.Paid, booking.Status);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldFail_IfNoSeatsAvailable()
    {
        var db = GetDbContext();
    var tourId = Guid.NewGuid();
        var tour = new Tour { Id = tourId, Title = "Test Tour", StartAt = DateTime.UtcNow, StartLocationText = "A", EndLocationText = "B", Status = TourStatus.Published };
    var tourMotard = new TourMotard { Id = Guid.NewGuid(), TourId = tourId, MotardUserId = "motard@test", CapacitySeats = 1, SeatsBooked = 1, RowVersion = DateTime.UtcNow };
    var booking = new Booking { Id = Guid.NewGuid(), TourId = tourId, TourMotardId = tourMotard.Id, TourMotard = tourMotard, ParticipantUserId = "participant@test", Status = BookingStatus.Approved, RowVersion = DateTime.UtcNow };
    db.Tours.Add(tour);
    db.TourMotards.Add(tourMotard);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var service = new PaymentService(db);
        var result = await service.ProcessPaymentAsync(booking.Id, 100m);
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldFail_IfBookingNotApproved()
    {
        var db = GetDbContext();
    var tourId = Guid.NewGuid();
        var tour = new Tour { Id = tourId, Title = "Test Tour", StartAt = DateTime.UtcNow, StartLocationText = "A", EndLocationText = "B", Status = TourStatus.Published };
    var tourMotard = new TourMotard { Id = Guid.NewGuid(), TourId = tourId, MotardUserId = "motard@test", CapacitySeats = 5, SeatsBooked = 0, RowVersion = DateTime.UtcNow };
    var booking = new Booking { Id = Guid.NewGuid(), TourId = tourId, TourMotardId = tourMotard.Id, TourMotard = tourMotard, ParticipantUserId = "participant@test", Status = BookingStatus.Pending, RowVersion = DateTime.UtcNow };
    db.Tours.Add(tour);
    db.TourMotards.Add(tourMotard);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var service = new PaymentService(db);
        var result = await service.ProcessPaymentAsync(booking.Id, 100m);
        Assert.False(result);
    }
}
