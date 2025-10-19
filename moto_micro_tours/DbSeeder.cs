using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours
{
    /// <summary>
    /// Seeds the database with a happy path scenario for development/testing.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            // Only seed if no users exist
            if (await db.Users.AnyAsync()) return;

            // 1. Create users
            var motard = new AppUser { UserName = "motard1", Email = "motard1@example.com", EmailConfirmed = true };
            var participant = new AppUser { UserName = "participant1", Email = "participant1@example.com", EmailConfirmed = true };
            db.Users.AddRange(motard, participant);
            await db.SaveChangesAsync();

            // 2. MotardProfile
            var motardProfile = new MotardProfile
            {
                UserId = motard.Id,
                Bio = "Experienced rider.",
                BikeModel = "Yamaha MT-07",
                LicenseNo = "MOTO123456",
                CompletedTours = 0
            };
            db.MotardProfiles.Add(motardProfile);
            await db.SaveChangesAsync();

            // 3. Tour
            var tour = new Tour
            {
                Title = "Alps Adventure",
                StartAt = DateTime.UtcNow.AddDays(7),
                StartLocationText = "Geneva, Switzerland",
                EndLocationText = "Turin, Italy",
                Status = TourStatus.Published
            };
            db.Tours.Add(tour);
            await db.SaveChangesAsync();

            // 4. TourMotard
            var tourMotard = new TourMotard
            {
                TourId = tour.Id,
                MotardUserId = motard.Id,
                CapacitySeats = 2,
                PricePerSeat = 150.00m,
                SeatsBooked = 0
            };
            db.TourMotards.Add(tourMotard);
            await db.SaveChangesAsync();

            // 5. PreBookingConversation
            var preConv = new PreBookingConversation
            {
                TourId = tour.Id,
                MotardUserId = motard.Id,
                ParticipantUserId = participant.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.PreBookingConversations.Add(preConv);
            await db.SaveChangesAsync();

            // 6. Message in PreBookingConversation
            var message = new Message
            {
                PreBookingConversationId = preConv.Id,
                SenderUserId = participant.Id,
                Content = "Hi, is this tour suitable for beginners?",
                SentAt = DateTime.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();

            // 7. Booking
            var booking = new Booking
            {
                TourId = tour.Id,
                TourMotardId = tourMotard.Id,
                ParticipantUserId = participant.Id,
                Status = BookingStatus.Approved,
                CancelDecision = CancelDecision.None,
                CostSharingOptIn = false,
                CreatedAt = DateTime.UtcNow,
            };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            // 8. BookingConversation
            var bookingConv = new BookingConversation
            {
                BookingId = booking.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.BookingConversations.Add(bookingConv);
            await db.SaveChangesAsync();

            // 9. Payment
            var payment = new Payment
            {
                BookingId = booking.Id,
                Amount = 150.00m,
                Status = PaymentStatus.Succeeded,
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            // 10. Payout
            var payout = new Payout
            {
                BookingId = booking.Id,
                PlatformShare = 15.00m,
                MotardShare = 135.00m,
                Status = PayoutStatus.Settled,
                CreatedAt = DateTime.UtcNow,
                SettledAt = DateTime.UtcNow
            };
            db.Payouts.Add(payout);
            await db.SaveChangesAsync();

            // 11. Review
            var review = new Review
            {
                MotardUserId = motard.Id,
                AuthorUserId = participant.Id,
                BookingId = booking.Id,
                Rating = 5,
                Comment = "Amazing tour! Highly recommended.",
                CreatedAt = DateTime.UtcNow
            };
            db.Reviews.Add(review);
            await db.SaveChangesAsync();
        }
    }
}
