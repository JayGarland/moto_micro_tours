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

            // Only seed if no tours exist (allow seeding even if users exist)
            if (await db.Tours.AnyAsync()) return;

            // 1. Create users
            var motardA = new AppUser { UserName = "motardA@test", Email = "motardA@test", EmailConfirmed = true };
            var motardB = new AppUser { UserName = "motardB@test", Email = "motardB@test", EmailConfirmed = true };
            var participant = new AppUser { UserName = "participant@test", Email = "participant@test", EmailConfirmed = true };
            db.Users.AddRange(motardA, motardB, participant);
            await db.SaveChangesAsync();

            // 2. MotardProfiles
            var profileA = new MotardProfile
            {
                UserId = motardA.Id,
                Bio = "Experienced rider with 10+ years on the road. Love scenic coastal routes!",
                BikeModel = "BMW R1250GS",
                LicenseNo = "FR-A12345",
                AvgRating = 4.8m,
                CompletedTours = 12
            };
            var profileB = new MotardProfile
            {
                UserId = motardB.Id,
                Bio = "Adventure touring enthusiast. Safety first, fun always!",
                BikeModel = "Honda Africa Twin",
                LicenseNo = "FR-B67890",
                AvgRating = 4.5m,
                CompletedTours = 8
            };
            db.MotardProfiles.AddRange(profileA, profileB);
            await db.SaveChangesAsync();

            // 3. Tour
            var tour = new Tour
            {
                Title = "Normandy Coast Loop",
                StartAt = new DateTime(2025, 10, 25, 10, 0, 0, DateTimeKind.Utc),
                StartLocationText = "Rouen Central Station",
                EndLocationText = "Rouen Central Station",
                Status = TourStatus.Published,
                Notes = "Scenic coastal ride with stops at historic D-Day beaches and Mont-Saint-Michel. Lunch included. Bring warm gear!"
            };
            db.Tours.Add(tour);
            await db.SaveChangesAsync();

            // 4. TourMotards (2 motards for the same tour)
            var tourMotardA = new TourMotard
            {
                TourId = tour.Id,
                MotardUserId = motardA.Id,
                CapacitySeats = 1,
                PricePerSeat = 20.00m,
                SeatsBooked = 0
            };
            var tourMotardB = new TourMotard
            {
                TourId = tour.Id,
                MotardUserId = motardB.Id,
                CapacitySeats = 3,
                PricePerSeat = 10.00m,
                SeatsBooked = 0
            };
            db.TourMotards.AddRange(tourMotardA, tourMotardB);
            await db.SaveChangesAsync();

            // 5. PreBookingConversation (participant chatting with motardA before booking)
            var preConv = new PreBookingConversation
            {
                TourId = tour.Id,
                MotardUserId = motardA.Id,
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
                Content = "Hi, what gear should I bring for the Normandy coast?",
                SentAt = DateTime.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();

            // 7. Create a couple of past bookings (for review demo)
            // Create another participant for variety
            var participant2 = new AppUser { UserName = "participant2@test", Email = "participant2@test", EmailConfirmed = true };
            db.Users.Add(participant2);
            await db.SaveChangesAsync();

            // Booking 1: Paid booking from participant to motardA (from a past tour)
            var pastTour = new Tour
            {
                Title = "Loire Valley Wine Route",
                StartAt = new DateTime(2025, 9, 15, 9, 0, 0, DateTimeKind.Utc), // Past date
                StartLocationText = "Tours Central Station",
                EndLocationText = "Tours Central Station",
                Status = TourStatus.Closed,
                Notes = "Completed tour"
            };
            db.Tours.Add(pastTour);
            await db.SaveChangesAsync();

            var pastTourMotardA = new TourMotard
            {
                TourId = pastTour.Id,
                MotardUserId = motardA.Id,
                CapacitySeats = 2,
                PricePerSeat = 25.00m,
                SeatsBooked = 2
            };
            db.TourMotards.Add(pastTourMotardA);
            await db.SaveChangesAsync();

            var booking1 = new Booking
            {
                TourId = pastTour.Id,
                TourMotardId = pastTourMotardA.Id,
                ParticipantUserId = participant.Id,
                Status = BookingStatus.Paid,
                CostSharingOptIn = true,
                EmergencyContact = "+33 6 12 34 56 78",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                ApprovedAt = DateTime.UtcNow.AddDays(-29),
                PaidAt = DateTime.UtcNow.AddDays(-29)
            };
            db.Bookings.Add(booking1);
            await db.SaveChangesAsync();

            var booking2 = new Booking
            {
                TourId = pastTour.Id,
                TourMotardId = pastTourMotardA.Id,
                ParticipantUserId = participant2.Id,
                Status = BookingStatus.Paid,
                CostSharingOptIn = false,
                EmergencyContact = "+33 6 98 76 54 32",
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                ApprovedAt = DateTime.UtcNow.AddDays(-27),
                PaidAt = DateTime.UtcNow.AddDays(-27)
            };
            db.Bookings.Add(booking2);
            await db.SaveChangesAsync();

            // 8. Add reviews for motardA
            var review1 = new Review
            {
                MotardUserId = motardA.Id,
                AuthorUserId = participant.Id,
                BookingId = booking1.Id,
                Rating = 5,
                Comment = "Excellent guide! Very knowledgeable about the Loire Valley and kept us safe throughout. Highly recommend!",
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            };

            var review2 = new Review
            {
                MotardUserId = motardA.Id,
                AuthorUserId = participant2.Id,
                BookingId = booking2.Id,
                Rating = 4,
                Comment = "Great experience overall. Bike was comfortable and the route was scenic. Only minor issue was running a bit behind schedule.",
                CreatedAt = DateTime.UtcNow.AddDays(-24)
            };

            db.Reviews.AddRange(review1, review2);
            await db.SaveChangesAsync();
        }
    }
}

