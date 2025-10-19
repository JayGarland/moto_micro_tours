using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderRef { get; set; }
    public DateTime? PaidAt { get; set; }

    public Booking? Booking { get; set; }
    public ICollection<PaymentKey> PaymentKeys { get; set; } = new List<PaymentKey>();
    public DateTime CreatedAt { get; set; }
}
