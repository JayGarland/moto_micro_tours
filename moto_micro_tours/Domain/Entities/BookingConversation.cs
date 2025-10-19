namespace moto_micro_tours.Domain.Entities;

public class BookingConversation
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Booking? Booking { get; set; }
    // navigation properties in Entity Framework to represent one to many relationships
    // The actual implementation is usually a List<T>
    public ICollection<Message> Messages { get; set; } = new List<Message>(); 


    
}
