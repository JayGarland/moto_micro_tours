using System.Threading.Tasks;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours.Services;

public interface IPaymentService
{
    /// <summary>
    /// Process a payment for a booking. Ensures transactional safety and
    /// optimistic concurrency via RowVersion.
    /// </summary>
    Task<bool> ProcessPaymentAsync(Guid bookingId, decimal amount);
}
