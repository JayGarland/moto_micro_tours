using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.DTOs;
using moto_micro_tours.Services;

namespace moto_micro_tours.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        AppDbContext context,
        IPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Process payment for an approved booking (mock implementation)
    /// Creates Payment, updates Booking to Paid, increments SeatsBooked, creates Payout (50/50)
    /// </summary>
    /// <param name="bookingId">ID of the approved booking</param>
    /// <returns>Payment confirmation with booking and payout details</returns>
    [HttpPost("pay/{bookingId}")]
    public async Task<ActionResult<PaymentConfirmationDto>> ProcessPayment(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Processing payment for booking {BookingId}", bookingId);

            // Load booking to get the amount to pay
            var booking = await _context.Bookings
                .Include(b => b.TourMotard)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", bookingId);
                return NotFound(new { error = $"Booking with ID {bookingId} not found" });
            }

            if (booking.TourMotard == null)
            {
                return BadRequest(new { error = "TourMotard information missing" });
            }

            var amount = booking.TourMotard.PricePerSeat;

            // Process payment using the service (transactional)
            var success = await _paymentService.ProcessPaymentAsync(bookingId, amount);

            if (!success)
            {
                _logger.LogWarning("Payment failed for booking {BookingId}", bookingId);

                // Reload booking to get current state for error message
                var currentBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (currentBooking?.Status != Domain.Enums.BookingStatus.Approved)
                {
                    return BadRequest(new
                    {
                        error = $"Payment failed. Booking must be in Approved status. Current status: {currentBooking?.Status}"
                    });
                }

                if (currentBooking?.ExpiresAt.HasValue == true &&
                    DateTime.UtcNow > currentBooking.ExpiresAt.Value)
                {
                    return BadRequest(new
                    {
                        error = "Payment failed. Payment window has expired.",
                        expiresAt = currentBooking.ExpiresAt
                    });
                }

                return BadRequest(new
                {
                    error = "Payment failed. Please try again or contact support."
                });
            }

            // Reload booking with all related data for response
            var paidBooking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.TourMotard)
                    .ThenInclude(tm => tm!.Motard)
                .Include(b => b.Participant)
                .Include(b => b.Payment)
                .Include(b => b.Payout)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (paidBooking == null)
            {
                return StatusCode(500, new { error = "Payment processed but booking not found" });
            }

            _logger.LogInformation(
                "Payment successful for booking {BookingId}. Payment: {PaymentId}, Payout: {PayoutId}",
                bookingId,
                paidBooking.Payment?.Id,
                paidBooking.Payout?.Id);

            var response = new PaymentConfirmationDto
            {
                Success = true,
                Message = "Payment processed successfully",
                BookingId = paidBooking.Id,
                BookingStatus = paidBooking.Status.ToString(),
                PaidAt = paidBooking.PaidAt,
                Payment = paidBooking.Payment != null ? new PaymentInfoDto
                {
                    PaymentId = paidBooking.Payment.Id,
                    Amount = paidBooking.Payment.Amount,
                    Status = paidBooking.Payment.Status.ToString(),
                    ProviderRef = paidBooking.Payment.ProviderRef,
                    PaidAt = paidBooking.Payment.PaidAt
                } : null,
                Payout = paidBooking.Payout != null ? new PayoutInfoDto
                {
                    PayoutId = paidBooking.Payout.Id,
                    PlatformShare = paidBooking.Payout.PlatformShare,
                    MotardShare = paidBooking.Payout.MotardShare,
                    Status = paidBooking.Payout.Status.ToString(),
                    SettledAt = paidBooking.Payout.SettledAt
                } : null,
                TourTitle = paidBooking.Tour?.Title ?? "Unknown",
                MotardName = paidBooking.TourMotard?.Motard?.UserName ?? "Unknown"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for booking {BookingId}", bookingId);
            return StatusCode(500, new { error = "An error occurred while processing the payment" });
        }
    }
}
