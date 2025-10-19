namespace moto_micro_tours.Domain.Entities;

// Represents a payment-scoped idempotency key. Used to associate a retry key with
// a Booking and (optionally) the resulting Payment entity. This is useful to prevent
// duplicate payments in the business domain and to record which idempotency keys
// successfully resulted in a payment.
// Example usage: when processing POST /payments/pay/{bookingId}, create a PaymentKey
// row with the request's Idempotency-Key. If a retry arrives with same key, check
// the PaymentKey and return the previously recorded result.
public class PaymentKey
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Booking? Booking { get; set; }
}
