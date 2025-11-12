using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using moto_micro_tours;
using moto_micro_tours.Authorization;
using moto_micro_tours.Domain.Entities;
using Xunit;

public class ChatAuthorizationTests
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

    [Fact]
    public async Task PreBookingConversationHandler_Allows_Participant()
    {
        var db = GetDbContext();
        var participantId = "participant@test";
        var motardId = "motard@test";

        var conv = new PreBookingConversation
        {
            Id = System.Guid.NewGuid(),
            TourId = System.Guid.NewGuid(),
            MotardUserId = motardId,
            ParticipantUserId = participantId,
            CreatedAt = System.DateTime.UtcNow
        };

        db.PreBookingConversations.Add(conv);
        await db.SaveChangesAsync();

        var requirement = new ConversationAccessRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, participantId) }, "Test"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, conv);

        var handler = new PreBookingConversationHandler();
        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PreBookingConversationHandler_Denies_Other()
    {
        var db = GetDbContext();
        var participantId = "participant@test";
        var motardId = "motard@test";

        var conv = new PreBookingConversation
        {
            Id = System.Guid.NewGuid(),
            TourId = System.Guid.NewGuid(),
            MotardUserId = motardId,
            ParticipantUserId = participantId,
            CreatedAt = System.DateTime.UtcNow
        };

        db.PreBookingConversations.Add(conv);
        await db.SaveChangesAsync();

        var requirement = new ConversationAccessRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "someoneelse") }, "Test"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, conv);

        var handler = new PreBookingConversationHandler();
        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task BookingConversationHandler_Allows_Participant_Or_Motard()
    {
        var db = GetDbContext();
        var participantId = "participant@test";
        var motardId = "motard@test";

        var tourMotard = new TourMotard { Id = System.Guid.NewGuid(), MotardUserId = motardId, CapacitySeats = 2, SeatsBooked = 0 };
        db.TourMotards.Add(tourMotard);
        await db.SaveChangesAsync();

        var booking = new Booking { Id = System.Guid.NewGuid(), TourId = System.Guid.NewGuid(), TourMotardId = tourMotard.Id, TourMotard = tourMotard, ParticipantUserId = participantId, Status = Domain.Enums.BookingStatus.Approved };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var conv = new BookingConversation { Id = System.Guid.NewGuid(), BookingId = booking.Id, CreatedAt = System.DateTime.UtcNow };
        db.BookingConversations.Add(conv);
        await db.SaveChangesAsync();

        var requirement = new ConversationAccessRequirement();

        // participant
        var userParticipant = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, participantId) }, "Test"));
        var contextParticipant = new AuthorizationHandlerContext(new[] { requirement }, userParticipant, conv);
        var handler = new BookingConversationHandler(db);
        await handler.HandleAsync(contextParticipant);
        Assert.True(contextParticipant.HasSucceeded);

        // motard
        var userMotard = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, motardId) }, "Test"));
        var contextMotard = new AuthorizationHandlerContext(new[] { requirement }, userMotard, conv);
        await handler.HandleAsync(contextMotard);
        Assert.True(contextMotard.HasSucceeded);
    }

    [Fact]
    public async Task BookingConversationHandler_Denies_Other()
    {
        var db = GetDbContext();
        var participantId = "participant@test";
        var motardId = "motard@test";

        var tourMotard = new TourMotard { Id = System.Guid.NewGuid(), MotardUserId = motardId, CapacitySeats = 2, SeatsBooked = 0 };
        db.TourMotards.Add(tourMotard);
        await db.SaveChangesAsync();

        var booking = new Booking { Id = System.Guid.NewGuid(), TourId = System.Guid.NewGuid(), TourMotardId = tourMotard.Id, TourMotard = tourMotard, ParticipantUserId = participantId, Status = Domain.Enums.BookingStatus.Approved };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var conv = new BookingConversation { Id = System.Guid.NewGuid(), BookingId = booking.Id, CreatedAt = System.DateTime.UtcNow };
        db.BookingConversations.Add(conv);
        await db.SaveChangesAsync();

        var requirement = new ConversationAccessRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "someoneelse") }, "Test"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, conv);

        var handler = new BookingConversationHandler(db);
        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
