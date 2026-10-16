using moto_micro_tours.Tests;
using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;
using moto_micro_tours.Services;
using Xunit;
// The BookingPaymentFlowsTests.cs file contains tests for several manual flow scenarios, including:

// Approve, Pay, and Confirm Flow:

// Tests the approval, payment, and confirmation of a booking, ensuring the booking status transitions correctly and associated entities (e.g., payments, payouts) are updated.
// Payment After Expiration:

// Verifies that payments made after the ExpiresAt time are rejected, and no changes are made to the booking or related entities.
// Motard Cancels After Payment:

// Simulates a refund process when a motard cancels a booking after payment, ensuring the booking status is updated, seats are decremented, and payment/payout statuses are adjusted.
// Tour Cancellation:

// Tests the cascading effects of a tour cancellation, ensuring paid bookings are refunded, pending bookings are canceled, and associated entities are updated.
public class BookingPaymentFlowsTests
{
    private AppDbContext GetDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Seed an FK-valid tour, motard and booking in one go.</summary>
    private static async Task<(Tour Tour, TourMotard TourMotard, Booking Booking, Booking PendingBooking)> SeedAsync(
        AppDbContext db, BookingStatus firstBookingStatus, int capacity = 5)
    {
        await TestData.SeedUsersAsync(db);
        var tour = TestData.NewTour();
        var tourMotard = TestData.NewTourMotard(tour.Id, capacity);
        var booking = TestData.NewBooking(tour.Id, tourMotard.Id, firstBookingStatus);
        booking.CreatedAt = DateTime.UtcNow;
        var pending = TestData.NewBooking(tour.Id, tourMotard.Id, BookingStatus.Pending);
        pending.CreatedAt = DateTime.UtcNow;
        db.Tours.Add(tour);
        db.TourMotards.Add(tourMotard);
        db.Bookings.AddRange(booking, pending);
        await db.SaveChangesAsync();
        return (tour, tourMotard, booking, pending);
    }

    [Fact]
    public async Task Approve_Pay_Confirm_Flow_Works()
    {
        // Arrange
        var db = GetDbContext();
        var (_, tourMotard, booking, _) = await SeedAsync(db, BookingStatus.Pending, capacity: 2);

        // Act: Approve
        booking.Status = BookingStatus.Approved;
        booking.ApprovedAt = DateTime.UtcNow;
        booking.ExpiresAt = booking.ApprovedAt.Value.AddHours(6);
        await db.SaveChangesAsync();

        // Act: Pay
        var paymentService = new PaymentService(db);
        var result = await paymentService.ProcessPaymentAsync(booking.Id, 50m);

        // Assert
        Assert.True(result);

        var updatedBooking = await db.Bookings.Include(b => b.TourMotard).FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Paid, updatedBooking.Status);
        Assert.NotNull(updatedBooking.PaidAt);
        Assert.Equal(1, updatedBooking.TourMotard!.SeatsBooked);

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        Assert.NotNull(payment);
        var payout = await db.Payouts.FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        Assert.NotNull(payout);
        Assert.Equal(PayoutStatus.Pending, payout!.Status);
    }

    [Fact]
    public async Task Pay_After_ExpiresAt_ShouldBeRejected()
    {
        // Arrange
        var db = GetDbContext();
        var (_, tourMotard, booking, _) = await SeedAsync(db, BookingStatus.Approved, capacity: 2);

        // Set ExpiresAt in the past
        booking.ApprovedAt = DateTime.UtcNow.AddHours(-10);
        booking.ExpiresAt = booking.ApprovedAt.Value.AddHours(6); // expired
        await db.SaveChangesAsync();

        // Act: Pay
        var paymentService = new PaymentService(db);
        var result = await paymentService.ProcessPaymentAsync(booking.Id, 50m);

        // Assert
        Assert.False(result);
        var updatedBooking = await db.Bookings.Include(b => b.TourMotard).FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Approved, updatedBooking.Status);
        Assert.Equal(0, updatedBooking.TourMotard!.SeatsBooked);
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        Assert.Null(payment);
    }

    [Fact]
    public async Task Motard_Cancels_PostPay_ShouldRefundAndDecrementSeat()
    {
        // Arrange: create and pay booking
        var db = GetDbContext();
        var (_, tourMotard, booking, _) = await SeedAsync(db, BookingStatus.Approved, capacity: 2);

        // Pay
        var paymentService = new PaymentService(db);
        var paymentResult = await paymentService.ProcessPaymentAsync(booking.Id, 80m);
        Assert.True(paymentResult);

        // Verify initial paid state
        var paidBooking = await db.Bookings.Include(b => b.TourMotard).FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Paid, paidBooking.Status);
        Assert.Equal(1, paidBooking.TourMotard!.SeatsBooked);

        // Act: Motard cancels after pay (simulate refund)
        // In real service: create refund, update Payment & Payout statuses, decrement seats
        var payment = await db.Payments.FirstAsync(p => p.BookingId == booking.Id);
        payment.Status = PaymentStatus.Refunded;
        payment.PaidAt = null;

        var payout = await db.Payouts.FirstAsync(p => p.BookingId == booking.Id);
        payout.Status = PayoutStatus.Failed;

        paidBooking.Status = BookingStatus.Refunded;
        paidBooking.RefundedAt = DateTime.UtcNow;
        paidBooking.TourMotard!.SeatsBooked = Math.Max(0, paidBooking.TourMotard.SeatsBooked - 1);

        await db.SaveChangesAsync();

        // Assert
        var updatedBooking = await db.Bookings.Include(b => b.TourMotard).FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Refunded, updatedBooking.Status);
        Assert.Equal(0, updatedBooking.TourMotard!.SeatsBooked);
        var updatedPayment = await db.Payments.FirstAsync(p => p.BookingId == booking.Id);
        Assert.Equal(PaymentStatus.Refunded, updatedPayment.Status);
        var updatedPayout = await db.Payouts.FirstAsync(p => p.BookingId == booking.Id);
        Assert.Equal(PayoutStatus.Failed, updatedPayout.Status);
    }

    [Fact]
    public async Task Tour_Cancel_ShouldRefundPaid_AndCancelOthers()
    {
        var db = GetDbContext();
        var (tour, tourMotard, bookingPaid, bookingPending) = await SeedAsync(db, BookingStatus.Approved, capacity: 5);

        // Pay bookingPaid
        var paymentService = new PaymentService(db);
        var paid = await paymentService.ProcessPaymentAsync(bookingPaid.Id, 120m);
        Assert.True(paid);

        // Simulate tour cancellation cascade
        tour.Status = TourStatus.Cancelled;
        // For each booking: if Paid -> refund; else -> cancel
        var bookings = await db.Bookings.Include(b => b.TourMotard).ToListAsync();
        foreach (var b in bookings)
        {
            if (b.Status == BookingStatus.Paid)
            {
                var p = await db.Payments.FirstAsync(x => x.BookingId == b.Id);
                p.Status = PaymentStatus.Refunded;
                p.PaidAt = null;
                var po = await db.Payouts.FirstAsync(x => x.BookingId == b.Id);
                po.Status = PayoutStatus.Failed;

                b.Status = BookingStatus.Refunded;
                b.RefundedAt = DateTime.UtcNow;
                b.TourMotard!.SeatsBooked = Math.Max(0, b.TourMotard.SeatsBooked - 1);
            }
            else
            {
                b.Status = BookingStatus.Cancelled;
                b.CancelledAt = DateTime.UtcNow;
            }
        }
        await db.SaveChangesAsync();

        // Assert
        var b1 = await db.Bookings.FirstAsync(b => b.Id == bookingPaid.Id);
        var b2 = await db.Bookings.FirstAsync(b => b.Id == bookingPending.Id);
        Assert.Equal(BookingStatus.Refunded, b1.Status);
        Assert.Equal(BookingStatus.Cancelled, b2.Status);
    }

    // TODO: Add (smoke test)tests to validate database integrity, including unique constraints, foreign keys, and relational key types.
    // TODO: Implement smoke tests to:
    // 1. Count rows in each table to verify seed data is present.
    // 2. Ensure unique indexes exist (e.g., TourId, MotardUserId, PreBooking unique key).
    // 3. Attempt to insert a Message with duplicate conversation IDs and verify it fails (constraint check).
}
