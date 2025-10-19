using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Example transactional payment flow. This demonstrates:
    /// - loading the booking and related TourMotard (to adjust seats)
    /// - creating a Payment and Payout
    /// - using a database transaction and optimistic concurrency via RowVersion
    /// </summary>
    public async Task<bool> ProcessPaymentAsync(Guid bookingId, decimal amount)
    {
        // Use explicit transaction for multi-row updates
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Load booking with related tour-motard and include RowVersion
            var booking = await _db.Bookings
                .Include(b => b.TourMotard)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return false;

            // Business checks
            if (booking.Status != Domain.Enums.BookingStatus.Approved)
                return false; // Only approved bookings can be paid

            // Check expiry
            if (booking.ExpiresAt.HasValue && DateTime.UtcNow > booking.ExpiresAt.Value)
                return false; // payment window expired

            var tourMotard = booking.TourMotard!;
            if (tourMotard.SeatsBooked >= tourMotard.CapacitySeats)
                return false; // no seats available

            // Create payment
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                Amount = amount,
                Status = Domain.Enums.PaymentStatus.Succeeded,
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.Payments.Add(payment);

            // Update booking & seats
            booking.PaidAt = DateTime.UtcNow;
            booking.Status = Domain.Enums.BookingStatus.Paid;
            tourMotard.SeatsBooked += 1;

            // Save changes; RowVersion will detect concurrent updates and throw
            await _db.SaveChangesAsync();

            // Simulate payout creation
            var payout = new Payout
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PlatformShare = Math.Round(amount * 0.5m, 2),
                MotardShare = Math.Round(amount * 0.5m, 2),
                Status = Domain.Enums.PayoutStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _db.Payouts.Add(payout);

            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            // Ideally call external payment gateway here and update Payment.Status.
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrency: someone else updated Booking or TourMotard
            // TODO: Implement retry logic or user-facing error
            await tx.RollbackAsync();
            return false;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
