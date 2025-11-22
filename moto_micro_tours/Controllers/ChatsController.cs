using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using moto_micro_tours.DTOs;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Services;
using moto_micro_tours.Hubs;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Stage F: All chat endpoints require authentication
public class ChatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMessageService _messageService;
    private readonly ILogger<ChatsController> _logger;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHubContext<BookingChatHub> _hubContext;

    public ChatsController(
        AppDbContext db,
        IMessageService messageService,
        ILogger<ChatsController> logger,
        IAuthorizationService authService,
        UserManager<AppUser> userManager,
        IHubContext<BookingChatHub> hubContext)
    {
        _db = db;
        _messageService = messageService;
        _logger = logger;
        _authService = authService;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Send a message in a pre-booking conversation (participant -> motard or vice versa).
    /// Stage F: Authenticated users only. Authorization handler ensures only conversation participants can send.
    /// </summary>
    [HttpPost("prebooking/send")]
    public async Task<ActionResult<MessageDto>> SendPreBookingMessage([FromBody] SendPreBookingMessageDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Stage F: Get the authenticated user
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            _logger.LogWarning("PreBooking message send attempt without authentication");
            return Unauthorized(new { error = "You must be logged in to send messages" });
        }

        var tour = await _db.Tours.FindAsync(request.TourId);
        if (tour == null) return NotFound(new { error = "Tour not found" });

        var motard = await _db.Users.FindAsync(request.MotardUserId);
        if (motard == null) return NotFound(new { error = "Motard user not found" });

        // Determine conversation participants based on who is sending
        // If current user is the motard, they need an existing conversation with a participant
        // If current user is a participant, create conversation with them as participant
        string participantId;
        string motardUserId = request.MotardUserId;

        if (currentUser.Id == request.MotardUserId)
        {
            // Current user is the motard - find existing conversation to reply to
            var existingConv = await _db.PreBookingConversations
                .Where(c => c.TourId == request.TourId && c.MotardUserId == request.MotardUserId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingConv == null)
            {
                return BadRequest(new { error = "No conversation found. Participant must initiate the conversation first." });
            }
            participantId = existingConv.ParticipantUserId;
        }
        else
        {
            // Current user is the participant
            participantId = currentUser.Id;
        }

        // Find or create conversation
        var conv = await _db.PreBookingConversations
            .FirstOrDefaultAsync(c => c.TourId == request.TourId
                && c.MotardUserId == motardUserId
                && c.ParticipantUserId == participantId);

        if (conv == null)
        {
            // Only participants can create new conversations
            if (currentUser.Id == request.MotardUserId)
            {
                return BadRequest(new { error = "Conversation does not exist" });
            }

            conv = new PreBookingConversation
            {
                Id = Guid.NewGuid(),
                TourId = request.TourId,
                MotardUserId = motardUserId,
                ParticipantUserId = participantId,
                CreatedAt = DateTime.UtcNow
            };
            _db.PreBookingConversations.Add(conv);
            await _db.SaveChangesAsync();
        }

        // Authorization: ensure caller is either motard or participant in the conversation
        var auth = await _authService.AuthorizeAsync(User, conv, "CanAccessPreBookingConversation");
        if (!auth.Succeeded)
        {
            _logger.LogWarning("User {UserId} unauthorized to send message to prebooking conversation {ConvId}",
                currentUser.Id, conv.Id);
            return Forbid();
        }

        var message = new Message
        {
            PreBookingConversationId = conv.Id,
            SenderUserId = currentUser.Id,
            Content = request.Content
        };

        var created = await _messageService.CreateMessageAsync(message);
        var dto = created.ToDto();
        return CreatedAtAction(nameof(GetPreBookingMessages), new { conversationId = conv.Id }, dto);
    }

    /// <summary>
    /// Get all prebooking conversations for the current authenticated user.
    /// Returns conversations where user is either the motard or participant.
    /// </summary>
    [HttpGet("prebooking/conversations")]
    public async Task<ActionResult<IEnumerable<object>>> GetMyPreBookingConversations()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized(new { error = "You must be logged in" });
        }

        var conversations = await _db.PreBookingConversations
            .Include(c => c.Tour)
            .Include(c => c.Motard)
            .Include(c => c.Participant)
            .Where(c => c.MotardUserId == currentUser.Id || c.ParticipantUserId == currentUser.Id)
            .Select(c => new
            {
                conversationId = c.Id,
                tourId = c.TourId,
                tourTitle = c.Tour!.Title,
                motardUserId = c.MotardUserId,
                motardName = c.Motard!.UserName ?? "Unknown",
                participantUserId = c.ParticipantUserId,
                participantName = c.Participant!.UserName ?? "Unknown",
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(conversations);
    }

    /// <summary>
    /// Get all booking conversations for the current authenticated user.
    /// Returns conversations where user is either the motard or participant.
    /// </summary>
    [HttpGet("booking/conversations")]
    public async Task<ActionResult<IEnumerable<object>>> GetMyBookingConversations()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized(new { error = "You must be logged in" });
        }

        var conversations = await _db.BookingConversations
            .Include(c => c.Booking)
                .ThenInclude(b => b!.TourMotard)
                    .ThenInclude(tm => tm!.Tour)
            .Include(c => c.Booking)
                .ThenInclude(b => b!.TourMotard)
                    .ThenInclude(tm => tm!.Motard)
            .Include(c => c.Booking)
                .ThenInclude(b => b!.Participant)
            .Where(c => c.Booking!.TourMotard!.MotardUserId == currentUser.Id
                     || c.Booking.ParticipantUserId == currentUser.Id)
            .Select(c => new
            {
                conversationId = c.Id,
                bookingId = c.BookingId,
                tourId = c.Booking!.TourMotard!.TourId,
                tourTitle = c.Booking.TourMotard.Tour!.Title,
                motardUserId = c.Booking.TourMotard.MotardUserId,
                motardName = c.Booking.TourMotard.Motard!.UserName ?? "Unknown",
                participantUserId = c.Booking.ParticipantUserId,
                participantName = c.Booking.Participant!.UserName ?? "Unknown",
                bookingStatus = c.Booking.Status.ToString(),
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(conversations);
    }

    /// <summary>
    /// List messages for a pre-booking conversation.
    /// Stage F: Only motard and participant in the conversation can access.
    /// </summary>
    [HttpGet("prebooking/{conversationId}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetPreBookingMessages(Guid conversationId, [FromQuery] DateTime? after = null, [FromQuery] int limit = 50)
    {
        var conv = await _db.PreBookingConversations.FindAsync(conversationId);
        if (conv == null) return NotFound(new { error = "Conversation not found" });

        // Authorization: ensure caller is either motard or participant in the conversation
        var auth = await _authService.AuthorizeAsync(User, conv, "CanAccessPreBookingConversation");
        if (!auth.Succeeded)
        {
            _logger.LogWarning("Unauthorized attempt to access prebooking conversation {ConvId}", conversationId);
            return Forbid();
        }

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
    /// Stage F: Authenticated users only. Authorization handler ensures only booking participants can send.
    /// </summary>
    [HttpPost("booking/send")]
    public async Task<ActionResult<MessageDto>> SendBookingMessage([FromBody] SendBookingMessageDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Stage F: Get the authenticated user
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            _logger.LogWarning("Booking message send attempt without authentication");
            return Unauthorized(new { error = "You must be logged in to send messages" });
        }

        var booking = await _db.Bookings
            .Include(b => b.TourMotard)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);
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

        // Authorization: ensure caller is participant or motard for the booking
        var auth = await _authService.AuthorizeAsync(User, conv, "CanAccessBookingConversation");
        if (!auth.Succeeded)
        {
            _logger.LogWarning("User {UserId} unauthorized to send message to booking conversation {ConvId}",
                currentUser.Id, conv.Id);
            return Forbid();
        }

        var message = new Message
        {
            BookingConversationId = conv.Id,
            SenderUserId = currentUser.Id,
            Content = request.Content
        };

        var created = await _messageService.CreateMessageAsync(message);
        var dto = created.ToDto();

        // Broadcast message via SignalR to all clients in the booking group
        await _hubContext.Clients.Group($"booking-{booking.Id}")
            .SendAsync("ReceiveMessage", dto);

        return CreatedAtAction(nameof(GetBookingMessages), new { conversationId = conv.Id }, dto);
    }

    /// <summary>
    /// List messages for a booking conversation.
    /// Stage F: Only motard and participant in the booking can access.
    /// </summary>
    [HttpGet("booking/{conversationId}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetBookingMessages(Guid conversationId, [FromQuery] DateTime? after = null, [FromQuery] int limit = 50)
    {
        var conv = await _db.BookingConversations.FindAsync(conversationId);
        if (conv == null) return NotFound(new { error = "Conversation not found" });

        // Authorization: ensure caller is either motard or participant in the booking
        var auth = await _authService.AuthorizeAsync(User, conv, "CanAccessBookingConversation");
        if (!auth.Succeeded)
        {
            _logger.LogWarning("Unauthorized attempt to access booking conversation {ConvId}", conversationId);
            return Forbid();
        }

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
