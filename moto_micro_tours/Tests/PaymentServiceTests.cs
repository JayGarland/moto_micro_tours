using moto_micro_tours.Tests;
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

    /// <summary>Seed an FK-valid published tour, motard and booking; returns the booking.</summary>
    private static async Task<(Tour Tour, TourMotard TourMotard, Booking Booking)> SeedApprovedBookingAsync(
        AppDbContext db, TourStatus tourStatus = TourStatus.Published, int capacity = 5, int seatsBooked = 0)
    {
        await TestData.SeedUsersAsync(db);
        var tour = TestData.NewTour(status: tourStatus);
        var tourMotard = TestData.NewTourMotard(tour.Id, capacity, seatsBooked);
        var booking = TestData.NewBooking(tour.Id, tourMotard.Id, BookingStatus.Approved);
        db.Tours.Add(tour);
        db.TourMotards.Add(tourMotard);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return (tour, tourMotard, booking);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldSucceed_ForValidBooking()
    {
        var db = GetDbContext();
        var (_, tourMotard, booking) = await SeedApprovedBookingAsync(db, capacity: 5);

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
        var (_, _, booking) = await SeedApprovedBookingAsync(db, capacity: 1, seatsBooked: 1);

        var service = new PaymentService(db);
        var result = await service.ProcessPaymentAsync(booking.Id, 100m);
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldFail_IfBookingNotApproved()
    {
        var db = GetDbContext();
        var (_, _, booking) = await SeedApprovedBookingAsync(db);
        booking.Status = BookingStatus.Pending;
        await db.SaveChangesAsync();

        var service = new PaymentService(db);
        var result = await service.ProcessPaymentAsync(booking.Id, 100m);
        Assert.False(result);
    }

}
