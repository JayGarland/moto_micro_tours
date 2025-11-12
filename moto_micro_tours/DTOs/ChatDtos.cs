using System;
using System.ComponentModel.DataAnnotations;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.DTOs
{
    public class SendPreBookingMessageDto
    {
        [Required]
        public Guid TourId { get; set; }

        [Required]
        public string MotardUserId { get; set; } = null!;

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = null!;
    }

    public class SendBookingMessageDto
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = null!;
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public string SenderUserId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
        // "prebooking" or "booking"
        public string ConversationType { get; set; } = null!;
        public Guid ConversationId { get; set; }
    }

    public class ConversationSummaryDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public Guid? TourId { get; set; }
        public Guid? BookingId { get; set; }
        public string CounterpartUserId { get; set; } = null!;
        public string LastMessagePreview { get; set; } = null!;
        public DateTime LastSentAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public static class ChatDtoExtensions
    {
        public static MessageDto ToDto(this Message m)
        {
            return new MessageDto
            {
                Id = m.Id,
                SenderUserId = m.SenderUserId,
                Content = m.Content,
                SentAt = m.SentAt,
                ConversationType = m.PreBookingConversationId != null ? "prebooking" : "booking",
                ConversationId = m.PreBookingConversationId ?? m.BookingConversationId ?? Guid.Empty
            };
        }
    }
}
