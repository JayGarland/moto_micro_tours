using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using moto_micro_tours.DTOs;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Services;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMessageService _messageService;
    private readonly ILogger<ChatsController> _logger;
    private readonly IAuthorizationService _authService;

    public ChatsController(AppDbContext db, IMessageService messageService, ILogger<ChatsController> logger, IAuthorizationService authService)
    {
        _db = db;
        _messageService = messageService;
        _logger = logger;
        _authService = authService;
    }

    /// <summary>
    /// Send a message in a pre-booking conversation (participant -> motard or vice versa).
    /// TODO: authentication is not yet implemented; this endpoint uses the seeded participant user as the sender for demo.
    /// </summary>
    [HttpPost("prebooking/send")]
    public async Task<ActionResult<MessageDto>> SendPreBookingMessage([FromBody] SendPreBookingMessageDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // For demo: use seeded participant user as sender
        var participantUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "participant@test");
        if (participantUser == null)
            return BadRequest(new { error = "Participant user not found. Please seed the DB or authenticate." });

        var tour = await _db.Tours.FindAsync(request.TourId);
        if (tour == null) return NotFound(new { error = "Tour not found" });

        var motard = await _db.Users.FindAsync(request.MotardUserId);
        if (motard == null) return NotFound(new { error = "Motard user not found" });

        // Find or create conversation (unique constraint enforced at DB level)
        var conv = await _db.PreBookingConversations
            .FirstOrDefaultAsync(c => c.TourId == request.TourId && c.MotardUserId == request.MotardUserId && c.ParticipantUserId == participantUser.Id);

        if (conv == null)
        {
            conv = new PreBookingConversation
            {
                Id = Guid.NewGuid(),
                TourId = request.TourId,
                MotardUserId = request.MotardUserId,
                ParticipantUserId = participantUser.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.PreBookingConversations.Add(conv);
            await _db.SaveChangesAsync();
        }

        var message = new Message
        {
            PreBookingConversationId = conv.Id,
            SenderUserId = participantUser.Id,
            Content = request.Content
        };

        // Authorization: ensure caller is either motard or participant in the conversation
        // TODO(STAGE F): When real authentication is added, remove the demo principal creation
        // and rely on HttpContext.User. Also, add [Authorize] attributes to controller/endpoints
        // (and call app.UseAuthentication() in Program.cs before UseAuthorization()).
        var principal = HttpContext.User?.Identity?.IsAuthenticated == true
            ? HttpContext.User
            : new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, participantUser.Id),
                new Claim(ClaimTypes.Name, participantUser.UserName ?? string.Empty)
            }, "Demo"));

        var auth = await _authService.AuthorizeAsync(principal, conv, "CanAccessPreBookingConversation");
        if (!auth.Succeeded)
        {
            _logger.LogWarning("Unauthorized attempt to send message to prebooking conversation {ConvId}", conv.Id);
            return Forbid();
        }

        var created = await _messageService.CreateMessageAsync(message);
        var dto = created.ToDto();
        return CreatedAtAction(nameof(GetPreBookingMessages), new { conversationId = conv.Id }, dto);
    }

    /// <summary>
    /// List messages for a pre-booking conversation
    /// </summary>
    [HttpGet("prebooking/{conversationId}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetPreBookingMessages(Guid conversationId, [FromQuery] DateTime? after = null, [FromQuery] int limit = 50)
    {
        var conv = await _db.PreBookingConversations.FindAsync(conversationId);
        if (conv == null) return NotFound(new { error = "Conversation not found" });

        // Use seeded principal when no authenticated user is present (demo mode)
        var participantUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "participant@test");
        var principal = HttpContext.User?.Identity?.IsAuthenticated == true
            ? HttpContext.User
            : new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, participantUser?.Id ?? string.Empty)
            }, "Demo"));

        var auth = await _authService.AuthorizeAsync(principal, conv, "CanAccessPreBookingConversation");
        if (!auth.Succeeded) return Forbid();

        var query = _db.Messages
            .Where(m => m.PreBookingConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .AsQueryable();

        if (after.HasValue) query = query.Where(m => m.SentAt > after.Value);
        query = query.Take(Math.Min(limit, 100));

        var msgs = await query.ToListAsync();
        return Ok(msgs.Select(m => m.ToDto()));
    }

    /// <summary>
    /// Send a message into a booking conversation. BookingConversation is created when a booking is Approved/Paid.
    /// </summary>
    [HttpPost("booking/send")]
    public async Task<ActionResult<MessageDto>> SendBookingMessage([FromBody] SendBookingMessageDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // For demo: use seeded participant user as sender
        var participantUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "participant@test");
        if (participantUser == null)
            return BadRequest(new { error = "Participant user not found. Please seed the DB or authenticate." });

        var booking = await _db.Bookings.FindAsync(request.BookingId);
        if (booking == null) return NotFound(new { error = "Booking not found" });

        // Only allow messages for Approved or Paid bookings (policy decision)
        if (booking.Status != Domain.Enums.BookingStatus.Approved && booking.Status != Domain.Enums.BookingStatus.Paid)
            return BadRequest(new { error = "Booking conversation not available until booking is Approved" });

        var conv = await _db.BookingConversations.FirstOrDefaultAsync(bc => bc.BookingId == booking.Id);
        if (conv == null)
        {
            conv = new BookingConversation
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.BookingConversations.Add(conv);
            await _db.SaveChangesAsync();
        }

        var message = new Message
        {
            BookingConversationId = conv.Id,
            SenderUserId = participantUser.Id,
            Content = request.Content
        };

        // Authorization: ensure caller is participant or motard for the booking
        var principal = HttpContext.User?.Identity?.IsAuthenticated == true
            ? HttpContext.User
            : new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, participantUser.Id),
                new Claim(ClaimTypes.Name, participantUser.UserName ?? string.Empty)
            }, "Demo"));

        var auth = await _authService.AuthorizeAsync(principal, conv, "CanAccessBookingConversation");
        if (!auth.Succeeded)
        {
            _logger.LogWarning("Unauthorized attempt to send message to booking conversation {ConvId}", conv.Id);
            return Forbid();
        }

        var created = await _messageService.CreateMessageAsync(message);
        var dto = created.ToDto();
        return CreatedAtAction(nameof(GetBookingMessages), new { conversationId = conv.Id }, dto);
    }

    /// <summary>
    /// List messages for a booking conversation
    /// </summary>
    [HttpGet("booking/{conversationId}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetBookingMessages(Guid conversationId, [FromQuery] DateTime? after = null, [FromQuery] int limit = 50)
    {
        var conv = await _db.BookingConversations.FindAsync(conversationId);
        if (conv == null) return NotFound(new { error = "Conversation not found" });

        // Use seeded principal when no authenticated user is present (demo mode)
        var participantUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "participant@test");
        var principal = HttpContext.User?.Identity?.IsAuthenticated == true
            ? HttpContext.User
            : new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, participantUser?.Id ?? string.Empty)
            }, "Demo"));

        var auth = await _authService.AuthorizeAsync(principal, conv, "CanAccessBookingConversation");
        if (!auth.Succeeded) return Forbid();

        var query = _db.Messages
            .Where(m => m.BookingConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .AsQueryable();

        if (after.HasValue) query = query.Where(m => m.SentAt > after.Value);
        query = query.Take(Math.Min(limit, 100));

        var msgs = await query.ToListAsync();
        return Ok(msgs.Select(m => m.ToDto()));
    }
}
