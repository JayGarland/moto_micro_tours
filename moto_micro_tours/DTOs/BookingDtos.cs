namespace moto_micro_tours.DTOs;

/// <summary>
/// Request DTO for creating a new booking
/// </summary>
public class CreateBookingRequestDto
{
    /// <summary>
    /// ID of the tour to book
    /// </summary>
    public Guid TourId { get; set; }

    /// <summary>
    /// ID of the specific motard (TourMotard) to book with
    /// </summary>
    public Guid TourMotardId { get; set; }

    /// <summary>
    /// Emergency contact information (required for safety)
    /// </summary>
    public string EmergencyContact { get; set; } = string.Empty;

    /// <summary>
    /// Whether participant opts in to cost-sharing (fuel/tolls)
    /// </summary>
    public bool CostSharingOptIn { get; set; }

    /// <summary>
    /// Optional additional message from participant
    /// </summary>
    public string? MessageToMotard { get; set; }
}

/// <summary>
/// Request DTO for declining a booking (Stage F7.1)
/// </summary>
public class DeclineBookingRequestDto
{
    /// <summary>
    /// Optional reason for declining the booking
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for approving effective start time (Phase 1, formerly Option E)
/// </summary>
public class ApproveStartTimeRequest
{
    /// <summary>
    /// Approved effective start time for completion gate
    /// </summary>
    public DateTime ApprovedStartAtUtc { get; set; }

    /// <summary>
    /// Optional reason for approving earlier start time
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Response DTO after creating a booking
/// Also used for booking list summaries in dashboards
/// </summary>
public class BookingResponseDto
{
    public Guid BookingId { get; set; }
    public Guid TourId { get; set; }
    public string TourTitle { get; set; } = string.Empty;
    public Guid TourMotardId { get; set; }
    public string MotardName { get; set; } = string.Empty;
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty; // Added for dashboard display
    public string Status { get; set; } = string.Empty;
    public bool CostSharingOptIn { get; set; }
    public string EmergencyContact { get; set; } = string.Empty;
    public string? MessageToMotard { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DeclinedAt { get; set; } // Stage F7.1
    public string? DeclineReason { get; set; } // Stage F7.1
    public DateTime TourStartAt { get; set; } // Added for filtering upcoming rides
    public decimal PricePerSeat { get; set; } // Added for price display
    public DateTime? CompletedByParticipantAtUtc { get; set; } // B1
    public DateTime? CompletedByMotardAtUtc { get; set; } // B1
    public DateTime? CompletedAtUtc { get; set; } // B1

    // Phase 1: Start time override audit
    public DateTime? ApprovedStartAtUtc { get; set; }
    public string? ApprovedStartReason { get; set; }
    public DateTime? ApprovedAtUtc { get; set; } // Phase 1: When approved
    public string? ApprovedByUserId { get; set; } // Phase 1: Who approved
}

/// <summary>
/// Detailed booking information including tour and payment details
/// Inherits all fields from BookingResponseDto and adds location, payment, and cancellation details
/// </summary>
public class BookingDetailDto : BookingResponseDto
{
    // Additional tour location information
    public string StartLocationText { get; set; } = string.Empty;
    public string EndLocationText { get; set; } = string.Empty;

    // Additional motard information
    public string MotardUserId { get; set; } = string.Empty;
    public string? OrganizerUserId { get; set; }

    // Additional booking status timestamps
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Payment information (if exists)
    public PaymentInfoDto? Payment { get; set; }

    // Payout information (if exists)
    public PayoutInfoDto? Payout { get; set; }
}

public class PaymentInfoDto
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProviderRef { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class PayoutInfoDto
{
    public Guid PayoutId { get; set; }
    public decimal PlatformShare { get; set; }
    public decimal MotardShare { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SettledAt { get; set; }
}

/// <summary>
/// Payment confirmation response after successful payment
/// </summary>
public class PaymentConfirmationDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public PaymentInfoDto? Payment { get; set; }
    public PayoutInfoDto? Payout { get; set; }
    public string TourTitle { get; set; } = string.Empty;
    public string MotardName { get; set; } = string.Empty;
}
