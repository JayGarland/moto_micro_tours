using moto_micro_tours.Domain.Enums;

namespace moto_micro_tours.Domain.Entities;

public class Payout
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal PlatformShare { get; set; }
    public decimal MotardShare { get; set; }
    public PayoutStatus Status { get; set; }
    public DateTime? SettledAt { get; set; }

    public Booking? Booking { get; set; }
    public DateTime CreatedAt { get; set; }
}
