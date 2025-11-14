using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;
//TODO: write a summary
namespace moto_micro_tours
{
    /// <summary>
    /// Seeds the database with a happy path scenario for development/testing.
    /// Stage F: Includes Identity roles and users with proper authentication setup.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await db.Database.MigrateAsync();

            // Only seed if no tours exist (allow seeding even if users exist)
            if (await db.Tours.AnyAsync()) return;

            // === STAGE F: Create Roles ===
            string[] roleNames = { "Organizer", "Motard", "Participant" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // === STAGE F: Create users with roles ===
            // Password for all demo users: "pass"

            // 1. Organizer (has BOTH Organizer + Motard roles, can create tours AND ride)
            var organizer = new AppUser
            {
                UserName = "organizer1",
                Email = "organizer1@test.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(organizer, "pass");
            await userManager.AddToRolesAsync(organizer, new[] { "Organizer", "Motard" });

            // 2. Pure Motards (only Motard role, can request to join tours)
            var motard1 = new AppUser
            {
                UserName = "motard1",
                Email = "motard1@test.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(motard1, "pass");
            await userManager.AddToRoleAsync(motard1, "Motard");

            var motard2 = new AppUser
            {
                UserName = "motard2",
                Email = "motard2@test.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(motard2, "pass");
            await userManager.AddToRoleAsync(motard2, "Motard");

            // 3. Participants (only Participant role, can only book tours)
            var participant1 = new AppUser
            {
                UserName = "participant1",
                Email = "participant1@test.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(participant1, "pass");
            await userManager.AddToRoleAsync(participant1, "Participant");

            var participant2 = new AppUser
            {
                UserName = "participant2",
                Email = "participant2@test.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(participant2, "pass");
            await userManager.AddToRoleAsync(participant2, "Participant");

            // 4. MotardProfiles (for organizer and motards who will ride)
            var organizerProfile = new MotardProfile
            {
                UserId = organizer.Id,
                Bio = "Tour organizer and experienced lead rider. 15+ years organizing group rides across France.",
                BikeModel = "BMW R1250GS Adventure",
                LicenseNo = "FR-ORG001",
                AvgRating = 4.9m,
                CompletedTours = 25
            };
            var profile1 = new MotardProfile
            {
                UserId = motard1.Id,
                Bio = "Experienced rider with 10+ years on the road. Love scenic coastal routes!",
                BikeModel = "BMW R1250GS",
                LicenseNo = "FR-M001",
                AvgRating = 4.8m,
                CompletedTours = 12
            };
            var profile2 = new MotardProfile
            {
                UserId = motard2.Id,
                Bio = "Adventure touring enthusiast. Safety first, fun always!",
                BikeModel = "Honda Africa Twin",
                LicenseNo = "FR-M002",
                AvgRating = 4.5m,
                CompletedTours = 8
            };
            db.MotardProfiles.AddRange(organizerProfile, profile1, profile2);
            await db.SaveChangesAsync();

            // 5. Tour (created by organizer, with organizer + motard1 as riders)
            var tour = new Tour
            {
                Title = "Normandy Coast Loop",
                StartAt = new DateTime(2025, 11, 25, 10, 0, 0, DateTimeKind.Utc),
                StartLocationText = "Rouen Central Station",
                EndLocationText = "Rouen Central Station",
                Status = TourStatus.Published,
                Notes = "Scenic coastal ride with stops at historic D-Day beaches and Mont-Saint-Michel. Lunch included. Bring warm gear!"
            };
            db.Tours.Add(tour);
            await db.SaveChangesAsync();

            // 6. TourMotards (organizer rides + motard1 joined/approved)
            var tourMotardOrganizer = new TourMotard
            {
                TourId = tour.Id,
                MotardUserId = organizer.Id,
                CapacitySeats = 1,
                PricePerSeat = 20.00m,
                SeatsBooked = 0
            };
            var tourMotard1 = new TourMotard
            {
                TourId = tour.Id,
                MotardUserId = motard1.Id,
                CapacitySeats = 3,
                PricePerSeat = 10.00m,
                SeatsBooked = 0
            };
            db.TourMotards.AddRange(tourMotardOrganizer, tourMotard1);
            await db.SaveChangesAsync();

            // 7. PreBookingConversation (participant1 chatting with organizer before booking)
            var preConv = new PreBookingConversation
            {
                TourId = tour.Id,
                MotardUserId = organizer.Id,
                ParticipantUserId = participant1.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.PreBookingConversations.Add(preConv);
            await db.SaveChangesAsync();

            // 8. Message in PreBookingConversation
            var message = new Message
            {
                PreBookingConversationId = preConv.Id,
                SenderUserId = participant1.Id,
                Content = "Hi, what gear should I bring for the Normandy coast?",
                SentAt = DateTime.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();

            // 9. Create a past tour with bookings (for review demo)
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

            var pastTourMotardOrg = new TourMotard
            {
                TourId = pastTour.Id,
                MotardUserId = organizer.Id,
                CapacitySeats = 2,
                PricePerSeat = 25.00m,
                SeatsBooked = 2
            };
            db.TourMotards.Add(pastTourMotardOrg);
            await db.SaveChangesAsync();

            var booking1 = new Booking
            {
                TourId = pastTour.Id,
                TourMotardId = pastTourMotardOrg.Id,
                ParticipantUserId = participant1.Id,
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
                TourMotardId = pastTourMotardOrg.Id,
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

            // 10. Add reviews for organizer (from past tour)
            var review1 = new Review
            {
                MotardUserId = organizer.Id,
                AuthorUserId = participant1.Id,
                BookingId = booking1.Id,
                Rating = 5,
                Comment = "Excellent guide! Very knowledgeable about the Loire Valley and kept us safe throughout. Highly recommend!",
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            };

            var review2 = new Review
            {
                MotardUserId = organizer.Id,
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

