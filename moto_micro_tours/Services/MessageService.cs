using moto_micro_tours.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace moto_micro_tours.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _db;
    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a message with robust XOR validation:
    /// Either PreBookingConversationId or BookingConversationId must be set, but not both or neither.
    /// </summary>
    public async Task<Message> CreateMessageAsync(Message message)
    {
        if (!IsValidXor(message))
            throw new ArgumentException("Message must have either PreBookingConversationId or BookingConversationId, but not both or neither.");

        // Optionally: check that referenced conversation exists
        if (message.PreBookingConversationId != null)
        {
            var exists = await _db.PreBookingConversations.FindAsync(message.PreBookingConversationId);
            if (exists == null)
                throw new ArgumentException("PreBookingConversation does not exist.");
        }
        if (message.BookingConversationId != null)
        {
            var exists = await _db.BookingConversations.FindAsync(message.BookingConversationId);
            if (exists == null)
                throw new ArgumentException("BookingConversation does not exist.");
        }

        message.Id = Guid.NewGuid();
        message.SentAt = DateTime.UtcNow;
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    private bool IsValidXor(Message message)
    {
        bool hasPre = message.PreBookingConversationId != null;
        bool hasBook = message.BookingConversationId != null;
        return hasPre ^ hasBook;
    }
}
